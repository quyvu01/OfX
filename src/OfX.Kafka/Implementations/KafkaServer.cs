using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OfX.Abstractions;
using OfX.Models;
using OfX.Attributes;
using OfX.Implementations;
using OfX.Kafka.Abstractions;
using OfX.Kafka.Constants;
using OfX.Kafka.Extensions;
using OfX.Kafka.Statics;
using OfX.Kafka.Wrappers;
using OfX.Responses;
using OfX.Configuration;
using OfX.Telemetry;

namespace OfX.Kafka.Implementations;

internal class KafkaServer<TModel, TAttribute> : IKafkaServer<TModel, TAttribute>, IDisposable
    where TAttribute : OfXAttribute
    where TModel : class
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConsumer<string, string> _consumer;
    private readonly IProducer<string, string> _producer;
    private readonly string _requestTopic;
    private readonly ILogger<KafkaServer<TModel, TAttribute>> _logger;
    private const string TransportName = "kafka";

    // Backpressure: limit concurrent processing (configurable via OfXConfigurator.SetMaxConcurrentProcessing)
    private readonly SemaphoreSlim _semaphore = new(OfXStatics.MaxConcurrentProcessing,
        OfXStatics.MaxConcurrentProcessing);

    private bool _topicsCreated;

    public KafkaServer(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        var kafkaBootstrapServers = KafkaStatics.KafkaHost;
        var consumerConfig = new ConsumerConfig
        {
            GroupId = OfXKafkaConstants.ServerGroupId,
            BootstrapServers = kafkaBootstrapServers,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        var producerConfig = new ProducerConfig { BootstrapServers = kafkaBootstrapServers };

        if (KafkaStatics.KafkaSslOptions != null)
        {
            KafkaStatics.SettingUpKafkaSsl(producerConfig);
            KafkaStatics.SettingUpKafkaSsl(consumerConfig);
        }

        _consumer = new ConsumerBuilder<string, string>(consumerConfig)
            .SetKeyDeserializer(Deserializers.Utf8)
            .SetValueDeserializer(Deserializers.Utf8)
            .Build();

        _producer = new ProducerBuilder<string, string>(producerConfig)
            .SetKeySerializer(Serializers.Utf8)
            .SetValueSerializer(Serializers.Utf8)
            .Build();
        _requestTopic = typeof(TAttribute).RequestTopic();
        _logger = serviceProvider.GetService<ILogger<KafkaServer<TModel, TAttribute>>>();
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        // Lazy topic creation
        if (!_topicsCreated)
        {
            await CreateTopicsAsync();
            _topicsCreated = true;
        }

        await Task.Yield();
        _consumer.Subscribe(_requestTopic);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = _consumer.Consume(cancellationToken);
                if (consumeResult?.Message == null) continue;

                // Backpressure - wait for available slot
                await _semaphore.WaitAsync(cancellationToken);
                try
                {
                    _ = ProcessMessageWithReleaseAsync(consumeResult, cancellationToken);
                }
                catch
                {
                    // If firing the task fails, release semaphore to prevent leak
                    _semaphore.Release();
                    throw;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Normal shutdown
                break;
            }
            catch (ConsumeException ex)
            {
                _logger?.LogError(ex, "Error consuming Kafka message for <{Attribute}>", typeof(TAttribute).Name);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error processing Kafka message for <{Attribute}>", typeof(TAttribute).Name);
            }
        }
    }

    private async Task ProcessMessageWithReleaseAsync(ConsumeResult<string, string> consumeResult,
        CancellationToken stoppingToken)
    {
        try
        {
            await ProcessMessageAsync(consumeResult, stoppingToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task ProcessMessageAsync(ConsumeResult<string, string> consumeResult, CancellationToken stoppingToken)
    {
        var messageUnWrapped = JsonSerializer
            .Deserialize<KafkaMessageWrapped<OfXRequest>>(consumeResult.Message.Value);

        // Extract parent trace context
        ActivityContext parentContext = default;
        if (consumeResult.Message.Headers != null)
        {
            var traceparentHeader = consumeResult.Message.Headers.FirstOrDefault(h => h.Key == "traceparent");
            if (traceparentHeader != null)
            {
                var traceparent = Encoding.UTF8.GetString(traceparentHeader.GetValueBytes());
                ActivityContext.TryParse(traceparent, null, out parentContext);
            }
        }

        var attributeName = typeof(TAttribute).Name;
        using var activity = OfXActivitySource.StartServerActivity(attributeName, parentContext);
        var stopwatch = Stopwatch.StartNew();

        // Create timeout CTS
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        cts.CancelAfter(OfXStatics.DefaultRequestTimeout);
        var cancellationToken = cts.Token;

        // Properly dispose the service scope
        using var serviceScope = _serviceProvider.CreateScope();

        try
        {
            activity?.SetMessagingTags(
                system: TransportName,
                destination: _requestTopic,
                messageId: consumeResult.Message.Key,
                operation: "process");

            OfXDiagnostics.MessageReceive(TransportName, _requestTopic, consumeResult.Message.Key);

            var pipeline = serviceScope.ServiceProvider
                .GetRequiredService<ReceivedPipelinesOrchestrator<TModel, TAttribute>>();

            var message = messageUnWrapped.Message;
            var query = new OfXQueryRequest<TAttribute>(message.SelectorIds, message.Expressions);
            var headers = consumeResult.Message.Headers?
                .ToDictionary(a => a.Key, h => Encoding.UTF8.GetString(h.GetValueBytes())) ?? [];

            var requestContext = new RequestContextImpl<TAttribute>(query, headers, cancellationToken);
            var data = await pipeline.ExecuteAsync(requestContext);

            var response = Result.Success(data);
            await SendResponseAsync(consumeResult, messageUnWrapped.ReplyTo, response, cancellationToken);

            // Record success metrics
            stopwatch.Stop();
            var itemCount = data?.Items?.Length ?? 0;

            OfXMetrics.RecordRequest(
                attributeName,
                TransportName,
                stopwatch.Elapsed.TotalMilliseconds,
                itemCount);

            activity?.SetOfXTags(message.Expressions, message.SelectorIds, itemCount);
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();

            _logger?.LogWarning("Request timeout for <{Attribute}>", attributeName);

            OfXMetrics.RecordError(
                attributeName,
                TransportName,
                stopwatch.Elapsed.TotalMilliseconds,
                "TimeoutException");

            activity?.SetStatus(ActivityStatusCode.Error, "Request timeout");

            var response = Result
                .Failed(new TimeoutException($"Request timeout for {attributeName}"));
            await TrySendResponseAsync(consumeResult, messageUnWrapped.ReplyTo, response, stoppingToken);
        }
        catch (Exception e)
        {
            stopwatch.Stop();

            _logger?.LogError(e, "Error while responding <{Attribute}>", attributeName);

            OfXMetrics.RecordError(
                attributeName,
                TransportName,
                stopwatch.Elapsed.TotalMilliseconds,
                e.GetType().Name);

            OfXDiagnostics.RequestError(attributeName, TransportName, e, stopwatch.Elapsed);

            activity?.RecordException(e);
            activity?.SetStatus(ActivityStatusCode.Error, e.Message);

            var response = Result.Failed(e);
            await TrySendResponseAsync(consumeResult, messageUnWrapped.ReplyTo, response, stoppingToken);
        }
        finally
        {
            // Always commit to avoid reprocessing - message has been handled (success or error)
            TryCommit(consumeResult);
        }
    }

    private async Task SendResponseAsync(ConsumeResult<string, string> consumeResult, string replyTo,
        Result response, CancellationToken cancellationToken)
    {
        await _producer.ProduceAsync(replyTo, new Message<string, string>
        {
            Key = consumeResult.Message.Key,
            Value = JsonSerializer.Serialize(response)
        }, cancellationToken);
    }

    private async Task TrySendResponseAsync(ConsumeResult<string, string> consumeResult, string replyTo,
        Result response, CancellationToken cancellationToken)
    {
        try
        {
            await SendResponseAsync(consumeResult, replyTo, response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to send response for <{Attribute}>", typeof(TAttribute).Name);
        }
    }

    private void TryCommit(ConsumeResult<string, string> consumeResult)
    {
        try
        {
            _consumer.Commit(consumeResult);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to commit offset for <{Attribute}>", typeof(TAttribute).Name);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        Dispose();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _consumer?.Dispose();
        _producer?.Dispose();
        _semaphore.Dispose();
    }

    private async Task CreateTopicsAsync()
    {
        const int numPartitions = 1;
        const short replicationFactor = 1;

        var config = new AdminClientConfig { BootstrapServers = KafkaStatics.KafkaHost };

        using var adminClient = new AdminClientBuilder(config).Build();

        try
        {
            var topicSpecification = new TopicSpecification
            {
                Name = _requestTopic,
                NumPartitions = numPartitions,
                ReplicationFactor = replicationFactor
            };

            await adminClient.CreateTopicsAsync([topicSpecification]);
        }
        catch (CreateTopicsException ex) when (ex.Results.Any(r => r.Error.Code == ErrorCode.TopicAlreadyExists))
        {
            // Topic already exists - this is fine
        }
        catch (CreateTopicsException ex)
        {
            _logger?.LogWarning(ex, "Failed to create Kafka topic {Topic}", _requestTopic);
        }
    }
}