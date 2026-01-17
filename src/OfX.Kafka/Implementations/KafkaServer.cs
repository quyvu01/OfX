using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OfX.Abstractions;
using OfX.ApplicationModels;
using OfX.Attributes;
using OfX.Implementations;
using OfX.Kafka.Abstractions;
using OfX.Kafka.Constants;
using OfX.Kafka.Extensions;
using OfX.Kafka.Statics;
using OfX.Kafka.Wrappers;
using OfX.Responses;
using OfX.Statics;

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

    // Backpressure: limit concurrent processing (configurable via OfXRegister.SetMaxConcurrentProcessing)
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
                _ = ProcessMessageWithReleaseAsync(consumeResult, cancellationToken);
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

        // Create timeout CTS
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        cts.CancelAfter(OfXStatics.DefaultRequestTimeout);
        var cancellationToken = cts.Token;

        // Properly dispose the service scope
        using var serviceScope = _serviceProvider.CreateScope();

        try
        {
            var pipeline = serviceScope.ServiceProvider
                .GetRequiredService<ReceivedPipelinesOrchestrator<TModel, TAttribute>>();

            var message = messageUnWrapped.Message;
            var query = new RequestOf<TAttribute>(message.SelectorIds, message.Expression);
            var headers = consumeResult.Message.Headers?
                .ToDictionary(a => a.Key, h => Encoding.UTF8.GetString(h.GetValueBytes())) ?? [];

            var requestContext = new RequestContextImpl<TAttribute>(query, headers, cancellationToken);
            var data = await pipeline.ExecuteAsync(requestContext);

            var response = Result.Success(data);
            await SendResponseAsync(consumeResult, messageUnWrapped.RelyTo, response, cancellationToken);

            // Commit offset after successful processing
            _consumer.Commit(consumeResult);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger?.LogWarning("Request timeout for <{Attribute}>", typeof(TAttribute).Name);
            var response = Result
                .Failed(new TimeoutException($"Request timeout for {typeof(TAttribute).Name}"));
            await SendResponseAsync(consumeResult, messageUnWrapped.RelyTo, response, stoppingToken);
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Error while responding <{Attribute}>", typeof(TAttribute).Name);
            var response = Result.Failed(e);
            await SendResponseAsync(consumeResult, messageUnWrapped.RelyTo, response, stoppingToken);
        }
    }

    private async Task SendResponseAsync(ConsumeResult<string, string> consumeResult, string replyTo,
        Result response, CancellationToken cancellationToken)
    {
        try
        {
            await _producer.ProduceAsync(replyTo, new Message<string, string>
            {
                Key = consumeResult.Message.Key,
                Value = JsonSerializer.Serialize(response)
            }, cancellationToken);

            // Still commit offset even on error to avoid reprocessing
            _consumer.Commit(consumeResult);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to send response for <{Attribute}>", typeof(TAttribute).Name);
        }
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