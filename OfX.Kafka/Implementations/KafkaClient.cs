using Confluent.Kafka;
using OfX.Abstractions;
using OfX.Attributes;
using OfX.Kafka.Abstractions;
using OfX.Responses;

namespace OfX.Kafka.Implementations;

public class KafkaClient : IKafkaClient
{
    private const string RequestTopic = "rpc-requests";
    private const string ResponseTopic = "rpc-responses";
    private const string KafkaBootstrapServers = "localhost:9092";

    public async Task<ItemsResponse<OfXDataResponse>> RequestAsync<TAttribute>(
        RequestContext<TAttribute> requestContext)
        where TAttribute : OfXAttribute
    {
        var correlationId = Guid.NewGuid().ToString();

        var producerConfig = new ProducerConfig { BootstrapServers = KafkaBootstrapServers };
        var consumerConfig = new ConsumerConfig
        {
            GroupId = $"rpc-client-group-{correlationId}",
            BootstrapServers = KafkaBootstrapServers,
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        using var producer = new ProducerBuilder<string, RequestOf<TAttribute>>(producerConfig).Build();
        using var consumer = new ConsumerBuilder<string, ItemsResponse<OfXDataResponse>>(consumerConfig).Build();

        // Subscribe to the response topic
        consumer.Subscribe(ResponseTopic);

        // Send the request
        await producer.ProduceAsync(RequestTopic, new Message<string, RequestOf<TAttribute>>
        {
            Key = correlationId,
            Value = requestContext.Query
        });

        Console.WriteLine($"Request sent with key {correlationId}");

        // Wait for the response
        var timeout = TimeSpan.FromSeconds(10);
        var cts = new CancellationTokenSource(timeout);
        try
        {
            while (true)
            {
                var consumeResult = consumer.Consume(cts.Token);
                if (consumeResult.Message.Key == correlationId)
                {
                    Console.WriteLine($"Response received: {consumeResult.Message.Value}");
                    return consumeResult.Message.Value;
                }
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Request timed out.");
            return null;
        }
    }
}