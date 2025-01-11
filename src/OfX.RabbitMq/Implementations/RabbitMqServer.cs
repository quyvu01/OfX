using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using OfX.Abstractions;
using OfX.ApplicationModels;
using OfX.Cached;
using OfX.Exceptions;
using OfX.Implementations;
using OfX.RabbitMq.Abstractions;
using OfX.RabbitMq.ApplicationModels;
using OfX.RabbitMq.Constants;
using OfX.RabbitMq.Extensions;
using OfX.Responses;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OfX.RabbitMq.Implementations;

internal class RabbitMqServer(IServiceProvider serviceProvider) : IRabbitMqServer
{
    public async Task ConsumeAsync()
    {
        var rabbitMqConfigurator = serviceProvider.GetRequiredService<RabbitMqConfigurator>();
        var queueName = $"{OfXRabbitMqConstants.QueueNamePrefix}-{AppDomain.CurrentDomain.FriendlyName.ToLower()}";
        const string routingKey = OfXRabbitMqConstants.RoutingKey;
        
        var credential = rabbitMqConfigurator.RabbitMqCredential;
        var userName = credential.RabbitMqUserName ?? OfXRabbitMqConstants.DefaultUserName;
        var password = credential.RabbitMqPassword ?? OfXRabbitMqConstants.DefaultPassword;
        var connectionFactory = new ConnectionFactory
        {
            HostName = rabbitMqConfigurator.RabbitMqHost, VirtualHost = rabbitMqConfigurator.RabbitVirtualHost,
            Port = rabbitMqConfigurator.RabbitMqPort,
            UserName = userName, Password = password
        };
        
        await using var connection = await connectionFactory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();
        await channel.QueueDeclareAsync(queue: queueName, durable: false, exclusive: false,
            autoDelete: false, arguments: null);
        
        var serviceTypes = typeof(IQueryOfHandler<,>);
        var handlers = OfXCached.AttributeMapHandler.Values.ToList();
        if (handlers is not { Count: > 0 }) return;

        var attributeTypes = handlers
            .Where(a => a.IsGenericType && a.GetGenericTypeDefinition() == serviceTypes)
            .Select(a => a.GetGenericArguments()[1]);
        
        foreach (var attributeType in attributeTypes)
        {
            var exchangeName = attributeType.GetExchangeName();
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

            var attributeType = Type.GetType(props.Type!);
            if (!OfXCached.AttributeMapHandler.TryGetValue(attributeType!, out var handlerType))
                throw new OfXException.CannotFindHandlerForOfAttribute(attributeType);

            var modelArg = handlerType.GetGenericArguments()[0];
            var serviceScope = serviceProvider.CreateScope();

            var pipeline = serviceScope.ServiceProvider
                .GetRequiredService(typeof(ReceivedPipelinesImpl<,>).MakeGenericType(modelArg, attributeType));

            var pipelineMethod = OfXCached.GetPipelineMethodByAttribute(pipeline, attributeType);

            var requestContextType = typeof(RequestContextImpl<>).MakeGenericType(attributeType);

            var queryType = typeof(RequestOf<>).MakeGenericType(attributeType);

            var message = JsonSerializer.Deserialize<MessageDeserializable>(Encoding.UTF8.GetString(body));

            var query = OfXCached.CreateInstanceWithCache(queryType, message.SelectorIds,
                message.Expression);
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
        await new TaskCompletionSource().Task;
    }
}