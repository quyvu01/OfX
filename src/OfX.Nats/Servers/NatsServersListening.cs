using Microsoft.Extensions.DependencyInjection;
using NATS.Net;
using OfX.Abstractions;
using OfX.Cached;
using OfX.Exceptions;
using OfX.Helpers;
using OfX.Implementations;
using OfX.Nats.Messages;
using OfX.Responses;

namespace OfX.Nats.Servers;

public class NatsServersListening(IServiceProvider serviceProvider)
{
    public async Task StartAsync()
    {
        var natsClient = serviceProvider.GetRequiredService<NatsClient>();
        var serviceTypes = typeof(IQueryOfHandler<,>);
        var handlers = OfXCached.AttributeMapHandler.Values.ToList();
        var attributeTypes = handlers
            .Where(a => a.IsGenericType && a.GetGenericTypeDefinition() == serviceTypes)
            .Select(a => a.GetGenericArguments()[1]);
        foreach (var attributeType in attributeTypes)
        {
            await foreach (var message in natsClient.SubscribeAsync<MessageRequestOf>(
                               attributeType.GetAssemblyName()))
            {
                if (message.Data is null) continue;
                if (!OfXCached.AttributeMapHandler.TryGetValue(attributeType, out var handlerType))
                    throw new OfXException.CannotFindHandlerForOfAttribute(attributeType);

                var modelArg = handlerType.GetGenericArguments()[0];

                var serviceScope = serviceProvider.CreateScope();

                var pipeline = serviceScope.ServiceProvider
                    .GetRequiredService(typeof(ReceivedPipelinesImpl<,>).MakeGenericType(modelArg, attributeType));

                var pipelineMethod = OfXCached.GetPipelineMethodByAttribute(pipeline, attributeType);

                var requestContextType = typeof(RequestContextImpl<>).MakeGenericType(attributeType);

                var queryType = typeof(RequestOf<>).MakeGenericType(attributeType);

                var query = OfXCached.CreateInstanceWithCache(queryType, message.Data.SelectorIds.ToList(),
                    message.Data.Expression);
                var headers = message.Headers?
                    .ToDictionary(a => a.Key, b => b.Value.ToString()) ?? [];
                var requestContext = Activator
                    .CreateInstance(requestContextType, query, headers, CancellationToken.None);
                // Invoke the method and get the result
                var response = await ((Task<ItemsResponse<OfXDataResponse>>)pipelineMethod!
                    .Invoke(pipeline, [requestContext]))!;
                await natsClient.PublishAsync(message.ReplyTo!, response);
            }
        }
    }
}