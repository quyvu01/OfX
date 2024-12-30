using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using OfX.Abstractions;
using OfX.Cached;
using OfX.Grpc.Exceptions;
using OfX.Implementations;
using OfX.Responses;

namespace OfX.Grpc.Servers;

public sealed class OfXGrpcServer(IServiceProvider serviceProvider) : OfXTransportService.OfXTransportServiceBase
{
    private const string GetDataAsync = nameof(GetDataAsync);

    private static readonly Lazy<ConcurrentDictionary<Type, MethodInfo>> MethodInfoStorage =
        new(() => new ConcurrentDictionary<Type, MethodInfo>());

    public override async Task<OfXItemsGrpcResponse> GetItems(GetOfXGrpcQuery request, ServerCallContext context)
    {
        try
        {
            var attributeType = Type.GetType(request.AttributeAssemblyType);

            if (attributeType is null)
                throw new OfXGrpcExceptions.CannotDeserializeOfXAttributeType(request.AttributeAssemblyType);

            if (!OfXCached.QueryMapHandler.TryGetValue(attributeType, out var handlerType))
                throw new OfXGrpcExceptions.CannotFindHandlerForOfAttribute(attributeType);

            var handler = serviceProvider.GetRequiredService(handlerType);

            var genericMethod = MethodInfoStorage.Value.GetOrAdd(attributeType, q => handler.GetType().GetMethods()
                .FirstOrDefault(m =>
                    m.Name == GetDataAsync && m.GetParameters() is { Length: 1 } parameters &&
                    parameters[0].ParameterType == typeof(RequestContext<>).MakeGenericType(q)));
            var requestContextType = typeof(RequestContextImpl<>).MakeGenericType(attributeType);

            var queryType = typeof(RequestOf<>).MakeGenericType(attributeType);

            var query = OfXCached.CreateInstanceWithCache(queryType, request.SelectorIds.ToList(),
                request.Expression);
            var headers = context.RequestHeaders.ToDictionary(k => k.Key, v => v.Value);
            var requestContext = Activator
                .CreateInstance(requestContextType, query, headers, context.CancellationToken);
            object[] arguments = [requestContext];
            // Invoke the method and get the result
            var requestTask = ((Task<ItemsResponse<OfXDataResponse>>)genericMethod
                .Invoke(handler, arguments))!;
            var response = await requestTask;
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