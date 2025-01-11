using Confluent.Kafka;
using OfX.Kafka.Abstractions;

namespace OfX.Kafka.Implementations;

public class KafkaServer : IKafkaServer
{
    private const string RequestTopic = "rpc-requests";
    private const string ResponseTopic = "rpc-responses";
    private const string KafkaBootstrapServers = "localhost:9092";

    public async Task StartAsync()
    {
        var config = new ConsumerConfig
        {
            GroupId = "rpc-server-group",
            BootstrapServers = KafkaBootstrapServers,
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        using var producer = new ProducerBuilder<string, string>(new ProducerConfig { BootstrapServers = KafkaBootstrapServers }).Build();

        consumer.Subscribe(RequestTopic);
        Console.WriteLine("Server is listening for RPC requests...");

        try
        {
            while (true)
            {
                var consumeResult = consumer.Consume(CancellationToken.None);
                Console.WriteLine($"Received request: {consumeResult.Message.Value}");

                // Process the request (simulate some logic)
                var result = $"Processed: {consumeResult.Message.Value}";

                // Send response back to the response topic
                await producer.ProduceAsync(ResponseTopic, new Message<string, string>
                {
                    Key = consumeResult.Message.Key, // Correlate response with request
                    Value = result
                });

                Console.WriteLine($"Response sent for key {consumeResult.Message.Key}");
            }
        }
        catch (OperationCanceledException)
        {
            consumer.Close();
        }
    }
}