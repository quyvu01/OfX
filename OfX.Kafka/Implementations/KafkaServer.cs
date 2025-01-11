using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using OfX.Abstractions;
using OfX.ApplicationModels;
using OfX.Attributes;
using OfX.Cached;
using OfX.Exceptions;
using OfX.Implementations;
using OfX.Kafka.Abstractions;
using OfX.Kafka.ApplicationModels;
using OfX.Kafka.Constants;
using OfX.Kafka.Extensions;
using OfX.Responses;

namespace OfX.Kafka.Implementations;

internal class KafkaServer<TAttribute>(IServiceProvider serviceProvider) : IKafkaServer<TAttribute>
    where TAttribute : OfXAttribute
{
    private const string RequestTopic = OfXKafkaConstants.RequestTopic;
    private const string ResponseTopic = OfXKafkaConstants.ResponseTopic;

    public async Task StartAsync()
    {
        var kafkaMqConfigurator = serviceProvider.GetRequiredService<KafkaMqConfigurator>();
        var kafkaBootstrapServers = kafkaMqConfigurator.KafkaHost;
        var config = new ConsumerConfig
        {
            GroupId = "rpc-server-group",
            BootstrapServers = kafkaBootstrapServers,
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        using var producer = new ProducerBuilder<string, string>(new ProducerConfig
            { BootstrapServers = kafkaBootstrapServers }).Build();

        consumer.Subscribe(RequestTopic);
        var routingKey = typeof(TAttribute).RoutingKey();

        try
        {
            while (true)
            {
                var consumeResult = consumer.Consume(CancellationToken.None);
                if (consumeResult.Message.Key != routingKey) continue;

                var attributeType = typeof(TAttribute);
                if (!OfXCached.AttributeMapHandler.TryGetValue(attributeType!, out var handlerType))
                    throw new OfXException.CannotFindHandlerForOfAttribute(attributeType);

                var modelArg = handlerType.GetGenericArguments()[0];
                var serviceScope = serviceProvider.CreateScope();

                var pipeline = serviceScope.ServiceProvider
                    .GetRequiredService(typeof(ReceivedPipelinesImpl<,>).MakeGenericType(modelArg, attributeType));

                var pipelineMethod = OfXCached.GetPipelineMethodByAttribute(pipeline, attributeType);

                var requestContextType = typeof(RequestContextImpl<>).MakeGenericType(attributeType);

                var queryType = typeof(RequestOf<>).MakeGenericType(attributeType);

                var message = JsonSerializer.Deserialize<MessageDeserializable>(consumeResult.Message.Value);

                var query = OfXCached.CreateInstanceWithCache(queryType, message.SelectorIds,
                    message.Expression);
                var headers = consumeResult.Message.Headers
                    .ToDictionary(a => a.Key, h => Encoding.UTF8.GetString(h.GetValueBytes()));
                var requestContext = Activator
                    .CreateInstance(requestContextType, query, headers, CancellationToken.None);
                // Invoke the method and get the result
                var response = await ((Task<ItemsResponse<OfXDataResponse>>)pipelineMethod!
                    .Invoke(pipeline, [requestContext]))!;

                await producer.ProduceAsync(ResponseTopic, new Message<string, string>
                {
                    Key = consumeResult.Message.Key,
                    Value = JsonSerializer.Serialize(response)
                });
            }
        }
        catch (OperationCanceledException)
        {
            consumer.Close();
        }
    }
}