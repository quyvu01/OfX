using System.Collections.Concurrent;
using System.Diagnostics;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using OfX.ApplicationModels;
using OfX.Cached;
using OfX.Exceptions;
using OfX.Grpc.Abstractions;
using OfX.Grpc.Exceptions;

namespace OfX.Grpc.Servers;

public sealed class OfXGrpcServer(IServiceProvider serviceProvider) : OfXTransportService.OfXTransportServiceBase
{
    private static readonly Lazy<ConcurrentDictionary<string, Type>>
        attributeAssemblyCached = new(() => []);

    public override async Task<OfXItemsGrpcResponse> GetItems(GetOfXGrpcQuery request, ServerCallContext context)
    {
        try
        {
            var serviceType = attributeAssemblyCached.Value.GetOrAdd(request.AttributeAssemblyType,
                attributeAssembly =>
                {
                    var ofXAttributeType = Type.GetType(attributeAssembly);
                    if (ofXAttributeType is null)
                        throw new OfXGrpcExceptions.CannotDeserializeOfXAttributeType(attributeAssembly);
                    if (!OfXCached.AttributeMapHandler.TryGetValue(ofXAttributeType, out var handlerType))
                        throw new OfXException.CannotFindHandlerForOfAttribute(ofXAttributeType);
                    var modelType = handlerType.GetGenericArguments()[0];
                    return typeof(IGrpcServer<,>).MakeGenericType(modelType, ofXAttributeType);
                });


            var grpcServer = serviceProvider.GetRequiredService(serviceType);
            if (grpcServer is not IGrpcServer server) throw new UnreachableException();

            var message = new MessageDeserializable
                { SelectorIds = [..request.SelectorIds], Expression = request.Expression };
            var headers = context.RequestHeaders.ToDictionary(k => k.Key, v => v.Value);
            var response = await server
                .GetResponse(message, headers, context.CancellationToken);
            var res = new OfXItemsGrpcResponse();
            response.Items.ForEach(a => res.Items.Add(new ItemGrpc { Id = a.Id, Value = a.Value }));
            return res;
        }
        catch (Exception e)
        {
            Debug.WriteLine($"Error while execute get items: {request.AttributeAssemblyType}, error: {e.Message}");
            throw;
        }
    }
}