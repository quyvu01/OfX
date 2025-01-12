using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.DependencyInjection;
using OfX.Abstractions;
using OfX.ApplicationModels;
using OfX.Attributes;
using OfX.Cached;
using OfX.Exceptions;
using OfX.Implementations;
using OfX.Kafka.Abstractions;
using OfX.Kafka.ApplicationModels;
using OfX.Kafka.Extensions;
using OfX.Kafka.Wrappers;
using OfX.Responses;

namespace OfX.Kafka.Implementations;

internal class KafkaServer<TAttribute> : IKafkaServer<TAttribute>, IDisposable
    where TAttribute : OfXAttribute
{
    private readonly KafkaConfigurator _kafkaConfigurator;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConsumer<string, string> _consumer;
    private readonly IProducer<string, string> _producer;
    private readonly string _requestTopic;

    public KafkaServer(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        var kafkaMqConfigurator = _serviceProvider.GetRequiredService<KafkaConfigurator>();
        _kafkaConfigurator = kafkaMqConfigurator;
        var kafkaBootstrapServers = kafkaMqConfigurator.KafkaHost;
        var consumerConfig = new ConsumerConfig
        {
            GroupId = "rpc-server-group",
            BootstrapServers = kafkaBootstrapServers,
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        var producerConfig = new ProducerConfig { BootstrapServers = kafkaBootstrapServers };

        _consumer = new ConsumerBuilder<string, string>(consumerConfig)
            .SetKeyDeserializer(Deserializers.Utf8)
            .SetValueDeserializer(Deserializers.Utf8)
            .Build();

        _producer = new ProducerBuilder<string, string>(producerConfig)
            .SetKeySerializer(Serializers.Utf8)
            .SetValueSerializer(Serializers.Utf8)
            .Build();
        _requestTopic = typeof(TAttribute).RequestTopic();
        CreateTopicsAsync().Wait();
    }

    public async Task StartAsync()
    {
        _consumer.Subscribe(_requestTopic);

        while (true)
        {
            try
            {
                var consumeResult = _consumer.Consume(CancellationToken.None);
                var messageUnWrapped = JsonSerializer
                    .Deserialize<KafkaMessageWrapped<MessageDeserializable>>(consumeResult.Message.Value);

                var attributeType = Type.GetType(messageUnWrapped.AttributeAssembly);
                if (!OfXCached.AttributeMapHandler.TryGetValue(attributeType!, out var handlerType))
                    throw new OfXException.CannotFindHandlerForOfAttribute(attributeType);

                var modelArg = handlerType.GetGenericArguments()[0];
                var serviceScope = _serviceProvider.CreateScope();

                var pipeline = serviceScope.ServiceProvider
                    .GetRequiredService(typeof(ReceivedPipelinesImpl<,>).MakeGenericType(modelArg, attributeType));

                var pipelineMethod = OfXCached.GetPipelineMethodByAttribute(pipeline, attributeType);

                var requestContextType = typeof(RequestContextImpl<>).MakeGenericType(attributeType);

                var queryType = typeof(RequestOf<>).MakeGenericType(attributeType);

                var message = messageUnWrapped.Message;

                var query = OfXCached.CreateInstanceWithCache(queryType, message.SelectorIds,
                    message.Expression);
                var headers = consumeResult.Message.Headers
                    .ToDictionary(a => a.Key, h => Encoding.UTF8.GetString(h.GetValueBytes()));
                var requestContext = Activator
                    .CreateInstance(requestContextType, query, headers, CancellationToken.None);
                // Invoke the method and get the result
                var response = await ((Task<ItemsResponse<OfXDataResponse>>)pipelineMethod!
                    .Invoke(pipeline, [requestContext]))!;

                await _producer.ProduceAsync(messageUnWrapped.RelyTo, new Message<string, string>
                {
                    Key = consumeResult.Message.Key,
                    Value = JsonSerializer.Serialize(response)
                });
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing request: {ex}");
            }
        }
    }

    public void Dispose()
    {
        _consumer?.Dispose();
        _producer?.Dispose();
    }

    public async Task CreateTopicsAsync()
    {
        const int numPartitions = 1;
        const short replicationFactor = 1;

        var config = new AdminClientConfig { BootstrapServers = _kafkaConfigurator.KafkaHost };

        using var adminClient = new AdminClientBuilder(config).Build();

        try
        {
            var topicSpecification = new TopicSpecification
            {
                Name = _requestTopic,
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