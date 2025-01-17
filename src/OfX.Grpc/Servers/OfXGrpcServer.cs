using System.Diagnostics;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using OfX.Abstractions;
using OfX.Cached;
using OfX.Exceptions;
using OfX.Grpc.Exceptions;
using OfX.Implementations;
using OfX.Responses;

namespace OfX.Grpc.Servers;

public sealed class OfXGrpcServer(IServiceProvider serviceProvider) : OfXTransportService.OfXTransportServiceBase
{
    public override async Task<OfXItemsGrpcResponse> GetItems(GetOfXGrpcQuery request, ServerCallContext context)
    {
        try
        {
            var attributeType = Type.GetType(request.AttributeAssemblyType);

            if (attributeType is null)
                throw new OfXGrpcExceptions.CannotDeserializeOfXAttributeType(request.AttributeAssemblyType);

            if (!OfXCached.AttributeMapHandler.TryGetValue(attributeType, out var handlerType))
                throw new OfXException.CannotFindHandlerForOfAttribute(attributeType);

            var modelArg = handlerType.GetGenericArguments()[0];

            var pipeline = serviceProvider
                .GetRequiredService(typeof(ReceivedPipelinesImpl<,>).MakeGenericType(modelArg, attributeType));

            var pipelineMethod = OfXCached.GetPipelineMethodByAttribute(pipeline, attributeType);

            var requestContextType = typeof(RequestContextImpl<>).MakeGenericType(attributeType);

            var queryType = typeof(RequestOf<>).MakeGenericType(attributeType);

            var query = OfXCached.CreateInstanceWithCache(queryType, request.SelectorIds.ToList(),
                request.Expression);
            var headers = context.RequestHeaders.ToDictionary(k => k.Key, v => v.Value);
            var requestContext = Activator
                .CreateInstance(requestContextType, query, headers, context.CancellationToken);
            object[] arguments = [requestContext];
            // Invoke the method and get the result
            var response = await ((Task<ItemsResponse<OfXDataResponse>>)pipelineMethod!
                .Invoke(pipeline, arguments))!;
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