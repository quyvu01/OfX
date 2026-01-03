using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OfX.ApplicationModels;
using OfX.Cached;
using OfX.Constants;
using OfX.Exceptions;
using OfX.Implementations;
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
        AttributeAssemblyCached = new(() => []);

    private IConnection _connection;
    private IChannel _channel;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        var queueName = $"{OfXRabbitMqConstants.QueueNamePrefix}-{AppDomain.CurrentDomain.FriendlyName.ToLower()}";
        const string routingKey = OfXRabbitMqConstants.RoutingKey;

        var userName = RabbitMqStatics.RabbitMqUserName ?? OfXRabbitMqConstants.DefaultUserName;
        var password = RabbitMqStatics.RabbitMqPassword ?? OfXRabbitMqConstants.DefaultPassword;
        var connectionFactory = new ConnectionFactory
        {
            HostName = RabbitMqStatics.RabbitMqHost, VirtualHost = RabbitMqStatics.RabbitVirtualHost,
            Port = RabbitMqStatics.RabbitMqPort, Ssl = RabbitMqStatics.SslOption ?? new SslOption(),
            UserName = userName, Password = password
        };

        _connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
        await _channel.QueueDeclareAsync(queue: queueName, durable: false, exclusive: false,
            autoDelete: false, arguments: null, cancellationToken: cancellationToken);

        var attributeTypes = OfXCached.AttributeMapHandlers.Keys.ToList();
        if (attributeTypes is not { Count: > 0 }) return;

        foreach (var exchangeName in attributeTypes.Select(attributeType => attributeType.GetExchangeName()))
        {
            await _channel.ExchangeDeclareAsync(exchangeName, type: ExchangeType.Direct,
                cancellationToken: cancellationToken);
            await _channel.QueueBindAsync(queue: queueName, exchangeName, routingKey,
                cancellationToken: cancellationToken);
        }

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (sender, ea) =>
        {
            var cons = (AsyncEventingBasicConsumer)sender;
            var ch = cons.Channel;
            var body = ea.Body.ToArray();
            var props = ea.BasicProperties;
            var replyProps = new BasicProperties { CorrelationId = props.CorrelationId };

            var receivedPipelineOrchestrator = AttributeAssemblyCached.Value.GetOrAdd(props.Type, attributeAssembly =>
            {
                var ofXAttributeType = Type.GetType(attributeAssembly)!;
                if (!OfXCached.AttributeMapHandlers.TryGetValue(ofXAttributeType, out var handlerType))
                    throw new OfXException.CannotFindHandlerForOfAttribute(ofXAttributeType);
                var modelType = handlerType.GetGenericArguments()[0];
                return typeof(ReceivedPipelinesOrchestrator<,>).MakeGenericType(modelType, ofXAttributeType);
            });

            using var scope = serviceProvider.CreateScope();
            var server = scope.ServiceProvider
                .GetService(receivedPipelineOrchestrator) as ReceivedPipelinesOrchestrator;
            ArgumentNullException.ThrowIfNull(server);
            try
            {
                var message = JsonSerializer.Deserialize<OfXRequest>(Encoding.UTF8.GetString(body));
                var headers = props.Headers?
                    .ToDictionary(a => a.Key, b => b.Value.ToString()) ?? [];
                var response = await server.ExecuteAsync(message, headers, ea.CancellationToken);
                var responseAsString = JsonSerializer.Serialize(response);
                var responseBytes = Encoding.UTF8.GetBytes(responseAsString);
                await ch.BasicPublishAsync(exchange: string.Empty, routingKey: props.ReplyTo!,
                    mandatory: true, basicProperties: replyProps, body: responseBytes,
                    cancellationToken: ea.CancellationToken);
            }
            catch (Exception e)
            {
                var logger = serviceProvider.GetService<ILogger<RabbitMqServer>>();
                logger.LogError("Error while responding <{@Attribute}> with message : {@Error}", props.Type, e);
                var responseAsString = JsonSerializer.Serialize(new ItemsResponse<OfXDataResponse>([]));
                var responseBytes = Encoding.UTF8.GetBytes(responseAsString);
                replyProps.Headers ??= new Dictionary<string, object>();
                replyProps.Headers.Add(OfXConstants.ErrorDetail, e.Message);
                await ch.BasicPublishAsync(exchange: string.Empty, routingKey: props.ReplyTo!,
                    mandatory: true, basicProperties: replyProps, body: responseBytes,
                    cancellationToken: ea.CancellationToken);
            }
            finally
            {
                await ch.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false,
                    cancellationToken: ea.CancellationToken);
            }
        };

        await _channel.BasicConsumeAsync(queueName, false, consumer, cancellationToken: cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _channel.Dispose();
        _connection.Dispose();
        return Task.CompletedTask;
    }
}