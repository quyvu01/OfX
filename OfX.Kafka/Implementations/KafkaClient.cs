using System.Text.Json;
using Confluent.Kafka;
using OfX.Abstractions;
using OfX.Attributes;
using OfX.Kafka.Abstractions;
using OfX.Kafka.ApplicationModels;
using OfX.Kafka.Constants;
using OfX.Kafka.Extensions;
using OfX.Responses;

namespace OfX.Kafka.Implementations;

internal class KafkaClient(KafkaMqConfigurator kafkaMqConfigurator) : IKafkaClient
{
    private const string RequestTopic = OfXKafkaConstants.RequestTopic;
    private const string ResponseTopic = OfXKafkaConstants.ResponseTopic;

    public async Task<ItemsResponse<OfXDataResponse>> RequestAsync<TAttribute>(
        RequestContext<TAttribute> requestContext) where TAttribute : OfXAttribute
    {
        var correlationId = Guid.NewGuid().ToString();
        var kafkaBootstrapServers = kafkaMqConfigurator.KafkaHost;

        var producerConfig = new ProducerConfig { BootstrapServers = kafkaBootstrapServers };
        var consumerConfig = new ConsumerConfig
        {
            GroupId = $"rpc-client-group-{correlationId}",
            BootstrapServers = kafkaBootstrapServers,
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        using var producer = new ProducerBuilder<string, string>(producerConfig).Build();
        using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();

        // Subscribe to the response topic
        consumer.Subscribe(ResponseTopic);

        // Send the request with the routing key
        await producer.ProduceAsync(RequestTopic, new Message<string, string>
        {
            Key = typeof(TAttribute).RoutingKey(), // Specify the routing key
            Value = JsonSerializer.Serialize(requestContext.Query)
        }, requestContext.CancellationToken);
        // Wait for the response
        try
        {
            while (true)
            {
                var consumeResult = consumer.Consume(requestContext.CancellationToken);
                if (consumeResult.Message.Key != correlationId) continue;
                return JsonSerializer.Deserialize<ItemsResponse<OfXDataResponse>>(consumeResult.Message.Value);
            }
        }
        catch (OperationCanceledException e)
        {
            Console.WriteLine(e.Message);
            return new ItemsResponse<OfXDataResponse>([]);
        }
    }
}