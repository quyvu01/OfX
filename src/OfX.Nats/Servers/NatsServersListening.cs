using Microsoft.Extensions.DependencyInjection;
using OfX.Abstractions;
using OfX.ApplicationModels;
using OfX.Cached;
using OfX.Exceptions;
using OfX.Implementations;
using OfX.Nats.Extensions;
using OfX.Nats.Wrappers;
using OfX.Responses;

namespace OfX.Nats.Servers;

internal static class NatsServersListening
{
    internal static void StartAsync(IServiceProvider serviceProvider)
    {
        var natsClient = serviceProvider.GetRequiredService<NatsClientWrapper>();
        var serviceTypes = typeof(IQueryOfHandler<,>);
        var handlers = OfXCached.AttributeMapHandler.Values.ToList();
        var attributeTypes = handlers
            .Where(a => a.IsGenericType && a.GetGenericTypeDefinition() == serviceTypes)
            .Select(a => a.GetGenericArguments()[1]).ToList();
        attributeTypes.ForEach(attributeType =>
        {
            if (!OfXCached.AttributeMapHandler.TryGetValue(attributeType, out var handlerType))
                throw new OfXException.CannotFindHandlerForOfAttribute(attributeType);
            var modelArg = handlerType.GetGenericArguments()[0];

            var receivedPipelineType = typeof(ReceivedPipelinesImpl<,>).MakeGenericType(modelArg, attributeType);
            var requestContextType = typeof(RequestContextImpl<>).MakeGenericType(attributeType);
            var queryType = typeof(RequestOf<>).MakeGenericType(attributeType);
            
            Task.Factory.StartNew(async () =>
            {
                var natsScribeAsync = natsClient.NatsClient
                    .SubscribeAsync<MessageDeserializable>(attributeType.GetNatsSubject());
                await foreach (var message in natsScribeAsync)
                {
                    if (message.Data is null) continue;
                    using var serviceScope = serviceProvider.CreateScope();
                    var pipeline = serviceScope.ServiceProvider
                        .GetRequiredService(receivedPipelineType);
                    var pipelineMethod = OfXCached.GetPipelineMethodByAttribute(pipeline, attributeType);
                    var query = OfXCached.CreateInstanceWithCache(queryType, message.Data.SelectorIds.ToList(),
                        message.Data.Expression);
                    var headers = message.Headers?
                        .ToDictionary(a => a.Key, b => b.Value.ToString()) ?? [];
                    var requestContext = Activator
                        .CreateInstance(requestContextType, query, headers, CancellationToken.None);
                    // Invoke the method and get the result
                    var response = await ((Task<ItemsResponse<OfXDataResponse>>)pipelineMethod!
                        .Invoke(pipeline, [requestContext]))!;
                    await natsClient.NatsClient.PublishAsync(message.ReplyTo!, response);
                }
            });
        });
    }
}