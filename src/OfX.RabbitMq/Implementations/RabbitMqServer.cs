using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OfX.ApplicationModels;
using OfX.Cached;
using OfX.Constants;
using OfX.Exceptions;
using OfX.RabbitMq.Abstractions;
using OfX.RabbitMq.Constants;
using OfX.RabbitMq.Extensions;
using OfX.RabbitMq.Statics;
using OfX.Responses;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OfX.RabbitMq.Implementations;

internal class RabbitMqServer(IServiceProvider serviceProvider) : IRabbitMqServer
{
    private static readonly Lazy<ConcurrentDictionary<string, Type>>
        attributeAssemblyCached = new(() => []);

    public async Task ConsumeAsync()
    {
        var queueName = $"{OfXRabbitMqConstants.QueueNamePrefix}-{AppDomain.CurrentDomain.FriendlyName.ToLower()}";
        const string routingKey = OfXRabbitMqConstants.RoutingKey;

        var userName = RabbitMqStatics.RabbitMqUserName ?? OfXRabbitMqConstants.DefaultUserName;
        var password = RabbitMqStatics.RabbitMqPassword ?? OfXRabbitMqConstants.DefaultPassword;
        var connectionFactory = new ConnectionFactory
        {
            HostName = RabbitMqStatics.RabbitMqHost, VirtualHost = RabbitMqStatics.RabbitVirtualHost,
            Port = RabbitMqStatics.RabbitMqPort,
            UserName = userName, Password = password
        };

        await using var connection = await connectionFactory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();
        await channel.QueueDeclareAsync(queue: queueName, durable: false, exclusive: false,
            autoDelete: false, arguments: null);

        var attributeTypes = OfXCached.AttributeMapHandlers.Keys.ToList();
        if (attributeTypes is not { Count: > 0 }) return;

        foreach (var exchangeName in attributeTypes.Select(attributeType => attributeType.GetExchangeName()))
        {
            await channel.ExchangeDeclareAsync(exchangeName, type: ExchangeType.Direct);
            await channel.QueueBindAsync(queue: queueName, exchangeName, routingKey);
        }

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (sender, ea) =>
        {
            var cons = (AsyncEventingBasicConsumer)sender;
            var ch = cons.Channel;
            var body = ea.Body.ToArray();
            var props = ea.BasicProperties;
            var replyProps = new BasicProperties { CorrelationId = props.CorrelationId };

            var rabbitMqServerRpcType = attributeAssemblyCached.Value.GetOrAdd(props.Type, attributeAssembly =>
            {
                var ofXAttributeType = Type.GetType(attributeAssembly)!;
                if (!OfXCached.AttributeMapHandlers.TryGetValue(ofXAttributeType, out var handlerType))
                    throw new OfXException.CannotFindHandlerForOfAttribute(ofXAttributeType);
                var modelType = handlerType.GetGenericArguments()[0];
                return typeof(IRabbitMqServerRpc<,>).MakeGenericType(modelType, ofXAttributeType);
            });

            using var scope = serviceProvider.CreateScope();

            var server = scope.ServiceProvider.GetService(rabbitMqServerRpcType);

            if (server is not IRabbitMqServerRpc serverRpc)
            {
                await ch.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                return;
            }

            try
            {
                var message = JsonSerializer.Deserialize<MessageDeserializable>(Encoding.UTF8.GetString(body));
                var headers = props.Headers?
                    .ToDictionary(a => a.Key, b => b.Value.ToString()) ?? [];
                var response = await serverRpc.GetResponseAsync(message, headers, ea.CancellationToken);
                var responseAsString = JsonSerializer.Serialize(response);
                var responseBytes = Encoding.UTF8.GetBytes(responseAsString);
                await ch.BasicPublishAsync(exchange: string.Empty, routingKey: props.ReplyTo!,
                    mandatory: true, basicProperties: replyProps, body: responseBytes);
            }
            catch (Exception e)
            {
                var logger = serviceProvider.GetService<ILogger<RabbitMqServer>>();
                var attributeType = Type.GetType(props.Type!);
                logger.LogError("Error while responding <{@Attribute}> with message : {@Error}",
                    attributeType?.Name, e);
                var responseAsString = JsonSerializer.Serialize(new ItemsResponse<OfXDataResponse>([]));
                var responseBytes = Encoding.UTF8.GetBytes(responseAsString);
                replyProps.Headers ??= new Dictionary<string, object>();
                replyProps.Headers.Add(OfXConstants.ErrorDetail, e.Message);
                await ch.BasicPublishAsync(exchange: string.Empty, routingKey: props.ReplyTo!,
                    mandatory: true, basicProperties: replyProps, body: responseBytes);
            }
            finally
            {
                await ch.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
            }
        };

        await channel.BasicConsumeAsync(queueName, false, consumer);
        await new TaskCompletionSource().Task;
    }
}