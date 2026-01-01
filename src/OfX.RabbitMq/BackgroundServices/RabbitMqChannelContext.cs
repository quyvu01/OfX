using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OfX.Abstractions.Agents;
using OfX.ApplicationModels;
using OfX.Cached;
using OfX.Constants;
using OfX.Exceptions;
using OfX.Implementations;
using OfX.RabbitMq.Constants;
using OfX.RabbitMq.Extensions;
using OfX.RabbitMq.Implementations;
using OfX.Responses;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OfX.RabbitMq.BackgroundServices;

public class RabbitMqChannelContext(IChannel channel, IServiceProvider serviceProvider) : IChannelContext
{
    private static readonly Lazy<ConcurrentDictionary<string, Type>>
        AttributeAssemblyCached = new(() => []);

    public async Task ConsumeAsync<T>(string queue, Func<T, Task> handler, CancellationToken cancellationToken)
    {
        const string routingKey = OfXRabbitMqConstants.RoutingKey;
        await channel.QueueDeclareAsync(queue: queue, durable: false, exclusive: false,
            autoDelete: false, arguments: null, cancellationToken: cancellationToken);

        var attributeTypes = OfXCached.AttributeMapHandlers.Keys.ToList();
        if (attributeTypes is not { Count: > 0 }) return;

        foreach (var exchangeName in attributeTypes.Select(attributeType => attributeType.GetExchangeName()))
        {
            await channel.ExchangeDeclareAsync(exchangeName, type: ExchangeType.Direct,
                cancellationToken: cancellationToken);
            await channel.QueueBindAsync(queue, exchangeName, routingKey,
                cancellationToken: cancellationToken);
        }

        var consumer = new AsyncEventingBasicConsumer(channel);

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

        await channel.BasicConsumeAsync(queue, false, consumer, cancellationToken: cancellationToken);
        await new TaskCompletionSource<object>().Task;
    }

    public Task PublishAsync<T>(string exchange, T message, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public ValueTask DisposeAsync()
    {
        channel.Dispose();
        return ValueTask.CompletedTask;
    }
}