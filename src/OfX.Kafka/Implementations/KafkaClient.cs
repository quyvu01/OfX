using System.Collections.Concurrent;
using System.Text.Json;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Logging;
using OfX.Abstractions;
using OfX.Abstractions.Transporting;
using OfX.ApplicationModels;
using OfX.Attributes;
using OfX.Exceptions;
using OfX.Kafka.Constants;
using OfX.Kafka.Extensions;
using OfX.Kafka.Statics;
using OfX.Kafka.Wrappers;
using OfX.Responses;
using OfX.Statics;

namespace OfX.Kafka.Implementations;

internal class KafkaClient : IRequestClient, IAsyncDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly IConsumer<string, string> _consumer;
    private readonly ILogger<KafkaClient> _logger;
    private readonly string _replyTo;
    private readonly CancellationTokenSource _consumerCts = new();
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _initialized;
    private Task _consumerTask;

    private readonly ConcurrentDictionary<string, TaskCompletionSource<Result>>
        _pendingRequests = new();

    public KafkaClient(ILogger<KafkaClient> logger)
    {
        _logger = logger;
        var kafkaBootstrapServers = KafkaStatics.KafkaHost;
        var producerConfig = new ProducerConfig { BootstrapServers = kafkaBootstrapServers };
        var consumerConfig = new ConsumerConfig
        {
            GroupId = $"{OfXKafkaConstants.ClientGroupId}-{Guid.NewGuid():N}",
            BootstrapServers = kafkaBootstrapServers,
            AutoOffsetReset = AutoOffsetReset.Latest,
            EnableAutoCommit = true
        };

        if (KafkaStatics.KafkaSslOptions != null)
        {
            KafkaStatics.SettingUpKafkaSsl(producerConfig);
            KafkaStatics.SettingUpKafkaSsl(consumerConfig);
        }

        _producer = new ProducerBuilder<string, string>(producerConfig)
            .SetKeySerializer(Serializers.Utf8)
            .SetValueSerializer(Serializers.Utf8)
            .Build();
        _consumer = new ConsumerBuilder<string, string>(consumerConfig)
            .SetKeyDeserializer(Deserializers.Utf8)
            .SetValueDeserializer(Deserializers.Utf8)
            .Build();
        _replyTo =
            $"{OfXKafkaConstants.ResponseTopicPrefix}-{AppDomain.CurrentDomain.FriendlyName.ToLower()}-{Guid.NewGuid():N}";
    }

    public async Task<ItemsResponse<DataResponse>> RequestAsync<TAttribute>(
        RequestContext<TAttribute> requestContext) where TAttribute : OfXAttribute
    {
        // Lazy initialization
        await EnsureInitializedAsync(requestContext.CancellationToken);

        var correlationId = Guid.NewGuid().ToString();
        var tcs = new TaskCompletionSource<Result>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingRequests.TryAdd(correlationId, tcs);

        try
        {
            // Produce the request
            var message = new KafkaMessageWrapped<OfXRequest>
            {
                Message = new OfXRequest(requestContext.Query.SelectorIds, requestContext.Query.Expression),
                ReplyTo = _replyTo
            };
            await _producer.ProduceAsync(typeof(TAttribute).RequestTopic(), new Message<string, string>
            {
                Key = correlationId,
                Value = JsonSerializer.Serialize(message)
            }, requestContext.CancellationToken);

            // Wait for response with timeout
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(requestContext.CancellationToken);
            cts.CancelAfter(OfXStatics.DefaultRequestTimeout);

            try
            {
                var response = await tcs.Task.WaitAsync(cts.Token);

                if (response is null)
                    throw new OfXException.ReceivedException("Received null response from server");

                if (!response.IsSuccess)
                    throw response.Fault?.ToException()
                          ?? new OfXException.ReceivedException("Unknown error from server");

                return response.Data;
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested &&
                                                     !requestContext.CancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException(
                    $"Timeout waiting for Kafka response for {typeof(TAttribute).Name}");
            }
        }
        finally
        {
            _pendingRequests.TryRemove(correlationId, out _);
        }
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (_initialized) return;

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_initialized) return;
            await CreateTopicsAsync();
            _consumerTask = Task.Run(() => StartConsume(_consumerCts.Token), _consumerCts.Token);
            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    private void StartConsume(CancellationToken cancellationToken)
    {
        _consumer.Subscribe(_replyTo);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = _consumer.Consume(cancellationToken);
                if (consumeResult?.Message == null) continue;

                if (!_pendingRequests.TryRemove(consumeResult.Message.Key, out var tcs)) continue;

                var response = JsonSerializer.Deserialize<Result>(consumeResult.Message.Value);
                tcs.TrySetResult(response);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Normal shutdown
                break;
            }
            catch (ConsumeException ex)
            {
                _logger?.LogError(ex, "Kafka consume error");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error processing Kafka response");
            }
        }
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
                Name = _replyTo,
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
            _logger?.LogWarning(ex, "Failed to create Kafka topic {Topic}", _replyTo);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_consumerTask != null)
        {
            try
            {
                await _consumerTask.WaitAsync(TimeSpan.FromSeconds(10));
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error waiting for consumer task shutdown");
            }
        }

        await CastAndDispose(_producer);
        await CastAndDispose(_consumer);
        await CastAndDispose(_consumerCts);
        await CastAndDispose(_initLock);
        await CastAndDispose(_consumerTask);
        // Cancel all pending requests
        foreach (var kvp in _pendingRequests) kvp.Value.TrySetCanceled();

        _pendingRequests.Clear();
        return;

        static async ValueTask CastAndDispose(IDisposable resource)
        {
            switch (resource)
            {
                case null:
                    return;
                case IAsyncDisposable resourceAsyncDisposable:
                    await resourceAsyncDisposable.DisposeAsync();
                    break;
                default:
                    resource.Dispose();
                    break;
            }
        }
    }
}