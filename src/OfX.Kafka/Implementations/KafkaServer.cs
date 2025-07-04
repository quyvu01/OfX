using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OfX.Abstractions;
using OfX.ApplicationModels;
using OfX.Attributes;
using OfX.Constants;
using OfX.Implementations;
using OfX.Kafka.Abstractions;
using OfX.Kafka.Constants;
using OfX.Kafka.Extensions;
using OfX.Kafka.Statics;
using OfX.Kafka.Wrappers;

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

    public KafkaServer(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        var kafkaBootstrapServers = KafkaStatics.KafkaHost;
        var consumerConfig = new ConsumerConfig
        {
            GroupId = OfXKafkaConstants.ServerGroupId,
            BootstrapServers = kafkaBootstrapServers,
            AutoOffsetReset = AutoOffsetReset.Earliest
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
        CreateTopicsAsync().Wait();
    }

    public async Task StartAsync()
    {
        await Task.Yield();
        _consumer.Subscribe(_requestTopic);

        while (true)
        {
            try
            {
                var consumeResult = _consumer.Consume(CancellationToken.None);
                _ = ProcessMessageAsync(consumeResult);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error processing request Kafka message: {@Error}", ex.Message);
            }
        }
    }

    private async Task ProcessMessageAsync(ConsumeResult<string, string> consumeResult)
    {
        var messageUnWrapped = JsonSerializer
            .Deserialize<KafkaMessageWrapped<MessageDeserializable>>(consumeResult.Message.Value);

        var serviceScope = _serviceProvider.CreateScope();

        var pipeline = serviceScope.ServiceProvider
            .GetRequiredService<ReceivedPipelinesOrchestrator<TModel, TAttribute>>();

        var message = messageUnWrapped.Message;

        var query = new RequestOf<TAttribute>(message.SelectorIds, message.Expression);
        var headers = consumeResult.Message.Headers
            .ToDictionary(a => a.Key, h => Encoding.UTF8.GetString(h.GetValueBytes()));

        var cancellationToken = CancellationToken.None;
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(OfXConstants.DefaultRequestTimeout);

        var requestContext = new RequestContextImpl<TAttribute>(query, headers, cts.Token);

        try
        {
            var response = await pipeline.ExecuteAsync(requestContext);
            var kafkaMessage = new KafkaMessage { Response = response, IsSucceed = true };
            await _producer.ProduceAsync(messageUnWrapped.RelyTo, new Message<string, string>
            {
                Key = consumeResult.Message.Key,
                Value = JsonSerializer.Serialize(kafkaMessage)
            }, cts.Token);
        }
        catch (Exception e)
        {
            var logger = serviceScope.ServiceProvider.GetService<ILogger<KafkaServer<TModel, TAttribute>>>();
            logger.LogError("Error while responding <{@Attribute}> with message : {@Error}",
                typeof(TAttribute).Name, e);
            var kafkaMessage = new KafkaMessage { ErrorDetail = e.Message, IsSucceed = false };
            await _producer.ProduceAsync(messageUnWrapped.RelyTo, new Message<string, string>
            {
                Key = consumeResult.Message.Key,
                Value = JsonSerializer.Serialize(kafkaMessage)
            }, cts.Token);
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

            // Create the topic
            await adminClient.CreateTopicsAsync([topicSpecification]);
        }
        catch (CreateTopicsException)
        {
            // ignore
        }
    }
}