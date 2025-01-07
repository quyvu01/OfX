using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using OfX.Abstractions;
using OfX.Cached;
using OfX.Exceptions;
using OfX.Implementations;
using OfX.RabbitMq.Abstractions;
using OfX.RabbitMq.ApplicationModels;
using OfX.RabbitMq.Constants;
using OfX.RabbitMq.Wrappers;
using OfX.Responses;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OfX.RabbitMq.Implementations;

public class RabbitMqServer(IServiceProvider serviceProvider) : IRabbitMqServer
{
    public async Task ConsumeAsync()
    {
        var connectionFactory = serviceProvider.GetRequiredService<RabbitMqClientWrapper>();
        const string queueName = OfXConstants.QueueName;
        await using var connection = await connectionFactory.ConnectionFactory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        await channel.QueueDeclareAsync(queue: queueName, durable: false, exclusive: false,
            autoDelete: false, arguments: null);

        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);

        var consumer = new AsyncEventingBasicConsumer(channel);

        //Todo: This is not correct because I must config the eventType!
        consumer.ReceivedAsync += async (sender, ea) =>
        {
            var cons = (AsyncEventingBasicConsumer)sender;
            var ch = cons.Channel;
            var body = ea.Body.ToArray();
            var props = ea.BasicProperties;
            var replyProps = new BasicProperties { CorrelationId = props.CorrelationId };
            var messageId = props.MessageId;
            var attributeType = Type.GetType(messageId!);
            if (attributeType is null || !OfXCached.AttributeMapHandler.TryGetValue(attributeType, out var handlerType))
                throw new OfXException.CannotFindHandlerForOfAttribute(attributeType);

            var modelArg = handlerType.GetGenericArguments()[0];

            var serviceScope = serviceProvider.CreateScope();

            var pipeline = serviceScope.ServiceProvider
                .GetRequiredService(typeof(ReceivedPipelinesImpl<,>).MakeGenericType(modelArg, attributeType));

            var pipelineMethod = OfXCached.GetPipelineMethodByAttribute(pipeline, attributeType);

            var requestContextType = typeof(RequestContextImpl<>).MakeGenericType(attributeType);

            var queryType = typeof(RequestOf<>).MakeGenericType(attributeType);

            var rabbitMqMessage = JsonSerializer.Deserialize<RabbitMqMessage>(Encoding.UTF8.GetString(body));

            var query = OfXCached.CreateInstanceWithCache(queryType, rabbitMqMessage.SelectorIds,
                rabbitMqMessage.Expression);
            var headers = props.Headers?
                .ToDictionary(a => a.Key, b => b.Value.ToString()) ?? [];
            var requestContext = Activator
                .CreateInstance(requestContextType, query, headers, CancellationToken.None);
            // Invoke the method and get the result
            var response = await ((Task<ItemsResponse<OfXDataResponse>>)pipelineMethod!
                .Invoke(pipeline, [requestContext]))!;
            try
            {
                var responseAsString = JsonSerializer.Serialize(response);
                var responseBytes = Encoding.UTF8.GetBytes(responseAsString);
                await ch.BasicPublishAsync(exchange: string.Empty, routingKey: props.ReplyTo!,
                    mandatory: true, basicProperties: replyProps, body: responseBytes);
            }
            finally
            {
                await ch.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
            }
        };

        await channel.BasicConsumeAsync(queueName, false, consumer);
    }
}