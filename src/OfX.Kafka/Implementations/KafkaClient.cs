using System.Collections.Concurrent;
using System.Text.Json;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using OfX.Abstractions;
using OfX.ApplicationModels;
using OfX.Attributes;
using OfX.Kafka.Abstractions;
using OfX.Kafka.Constants;
using OfX.Kafka.Extensions;
using OfX.Kafka.Statics;
using OfX.Kafka.Wrappers;
using OfX.Responses;

namespace OfX.Kafka.Implementations;

internal class KafkaClient : IKafkaClient, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly IConsumer<string, string> _consumer;
    private readonly string _relyTo;

    private readonly ConcurrentDictionary<string, TaskCompletionSource<ItemsResponse<OfXDataResponse>>>
        _pendingRequests = [];

    public KafkaClient()
    {
        var kafkaBootstrapServers = KafkaStatics.KafkaHost;
        var producerConfig = new ProducerConfig { BootstrapServers = kafkaBootstrapServers };
        var consumerConfig = new ConsumerConfig
        {
            GroupId = OfXKafkaConstants.ClientGroupId,
            BootstrapServers = kafkaBootstrapServers,
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        _producer = new ProducerBuilder<string, string>(producerConfig)
            .SetKeySerializer(Serializers.Utf8)
            .SetValueSerializer(Serializers.Utf8)
            .Build();
        _consumer = new ConsumerBuilder<string, string>(consumerConfig)
            .SetKeyDeserializer(Deserializers.Utf8)
            .SetValueDeserializer(Deserializers.Utf8)
            .Build();
        _relyTo = $"{OfXKafkaConstants.ResponseTopicPrefix}-{AppDomain.CurrentDomain.FriendlyName.ToLower()}";
        CreateTopicsAsync().Wait();
        Task.Factory.StartNew(StartConsume);
    }


    private void StartConsume()
    {
        _consumer.Subscribe(_relyTo);
        while (true)
        {
            try
            {
                var consumeResult = _consumer.Consume();
                if (!_pendingRequests.TryGetValue(consumeResult.Message.Key, out var tcs)) continue;
                var response = JsonSerializer
                    .Deserialize<ItemsResponse<OfXDataResponse>>(consumeResult.Message.Value);
                tcs.TrySetResult(response);
            }
            catch (Exception)
            {
                // ignore
            }
        }
    }


    public async Task<ItemsResponse<OfXDataResponse>> RequestAsync<TAttribute>(
        RequestContext<TAttribute> requestContext) where TAttribute : OfXAttribute
    {
        var correlationId = Guid.NewGuid().ToString();
        var tcs = new TaskCompletionSource<ItemsResponse<OfXDataResponse>>();
        _pendingRequests.TryAdd(correlationId, tcs);

        try
        {
            // Produce the request
            var message = new KafkaMessageWrapped<MessageDeserializable>
            {
                Message = new MessageDeserializable
                {
                    SelectorIds = requestContext.Query.SelectorIds, Expression = requestContext.Query.Expression,
                },
                RelyTo = _relyTo
            };
            await _producer.ProduceAsync(typeof(TAttribute).RequestTopic(), new Message<string, string>
            {
                Key = correlationId,
                Value = JsonSerializer.Serialize(message)
            });

            // Wait for response with timeout
            return await tcs.Task.WaitAsync(TimeSpan.FromSeconds(30));
        }
        finally
        {
            _pendingRequests.TryRemove(correlationId, out _);
        }
    }

    public void Dispose()
    {
        _producer?.Dispose();
        _consumer?.Dispose();
    }

    public async Task CreateTopicsAsync()
    {
        const int numPartitions = 1;
        const short replicationFactor = 1;

        var config = new AdminClientConfig { BootstrapServers = KafkaStatics.KafkaHost };

        using var adminClient = new AdminClientBuilder(config).Build();

        try
        {
            var topicSpecification = new TopicSpecification
            {
                Name = _relyTo,
                NumPartitions = numPartitions,
                ReplicationFactor = replicationFactor
            };

            // Create the topic
            await adminClient.CreateTopicsAsync([topicSpecification]);
        }
        catch (CreateTopicsException)
        {
            // ignore
        }
    }
}