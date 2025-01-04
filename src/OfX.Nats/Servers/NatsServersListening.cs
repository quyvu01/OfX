using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using NATS.Client;
using OfX.Abstractions;
using OfX.Cached;
using OfX.Exceptions;
using OfX.Extensions;
using OfX.Helpers;
using OfX.Implementations;
using OfX.Nats.Messages;
using OfX.Responses;

namespace OfX.Nats.Servers;

public class NatsServersListening(IServiceProvider serviceProvider)
{
    public void StartAsync()
    {
        var connection = serviceProvider.GetRequiredService<IConnection>();
        var serviceTypes = typeof(IQueryOfHandler<,>);
        var handlers = OfXCached.AttributeMapHandler.Values.ToList();
        handlers.Where(a => a.IsGenericType && a.GetGenericTypeDefinition() == serviceTypes)
            .Select(a => a.GetGenericArguments()[1])
            .ForEach(attributeType => connection.SubscribeAsync(attributeType.GetAssemblyName(), (_, args) =>
            {
                var request = Encoding.UTF8.GetString(args.Message.Data);
                var messageDeserialize = JsonSerializer.Deserialize<NatsMessageReceived>(request);

                if (!OfXCached.AttributeMapHandler.TryGetValue(attributeType, out var handlerType))
                    throw new OfXException.CannotFindHandlerForOfAttribute(attributeType);

                var modelArg = handlerType.GetGenericArguments()[0];

                var pipeline = serviceProvider
                    .GetRequiredService(typeof(ReceivedPipelinesImpl<,>).MakeGenericType(modelArg, attributeType));

                var pipelineMethod = OfXCached.GetPipelineMethodByAttribute(pipeline, attributeType);

                var requestContextType = typeof(RequestContextImpl<>).MakeGenericType(attributeType);

                var queryType = typeof(RequestOf<>).MakeGenericType(attributeType);

                var query = OfXCached.CreateInstanceWithCache(queryType, messageDeserialize.Query.SelectorIds.ToList(),
                    messageDeserialize.Query.Expression);
                var headers = messageDeserialize.Headers;
                var requestContext = Activator
                    .CreateInstance(requestContextType, query, headers, CancellationToken.None);
                // Invoke the method and get the result
                var response = ((Task<ItemsResponse<OfXDataResponse>>)pipelineMethod!
                    .Invoke(pipeline, [requestContext]))!;
                var result = JsonSerializer.Serialize(response.Result);
                connection.Publish(args.Message.Reply, Encoding.UTF8.GetBytes(result));
            }));
    }
}