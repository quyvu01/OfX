using System.Collections.Concurrent;
using System.Diagnostics;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using OfX.Abstractions;
using OfX.ApplicationModels;
using OfX.Cached;
using OfX.Exceptions;
using OfX.Extensions;
using OfX.Grpc.Exceptions;
using OfX.Implementations;

namespace OfX.Grpc.Servers;

public sealed class OfXGrpcServer(IServiceProvider serviceProvider) : OfXTransportService.OfXTransportServiceBase
{
    private readonly Lazy<ConcurrentDictionary<string, Type>> attributeAssemblyTypeMapReceivedPipelines = new(() => []);

    public override async Task<OfXItemsGrpcResponse> GetItems(GetOfXGrpcQuery request, ServerCallContext context)
    {
        try
        {
            var receivedPipelineType = attributeAssemblyTypeMapReceivedPipelines.Value
                .GetOrAdd(request.AttributeAssemblyType, attributeAssemblyType =>
                {
                    var attributeType = Type.GetType(attributeAssemblyType);

                    if (attributeType is null)
                        throw new OfXGrpcExceptions.CannotDeserializeOfXAttributeType(attributeAssemblyType);

                    if (!OfXCached.AttributeMapHandlers.TryGetValue(attributeType, out var handlerType))
                        throw new OfXException.CannotFindHandlerForOfAttribute(attributeType);

                    var modelArg = handlerType.GetGenericArguments()[0];
                    return typeof(ReceivedPipelinesOrchestrator<,>).MakeGenericType(modelArg, attributeType);
                });

            var pipeline = serviceProvider
                .GetRequiredService(receivedPipelineType);

            if (pipeline is not IReceivedPipelinesBase receivedPipelinesBase) throw new UnreachableException();

            var headers = context.RequestHeaders.ToDictionary(k => k.Key, v => v.Value);

            var message = new MessageDeserializable
            {
                SelectorIds = request.SelectorIds.ToList(), Expression = request.Expression
            };
            var response = await receivedPipelinesBase.ExecuteAsync(message, headers, context.CancellationToken);

            var res = new OfXItemsGrpcResponse();
            response.Items.ForEach(a =>
            {
                var itemGrpc = new ItemGrpc { Id = a.Id };
                a.OfXValues.ForEach(x =>
                    itemGrpc.OfxValues.Add(new OfXValueItemGrpc { Expression = x.Expression, Value = x.Value }));
                res.Items.Add(itemGrpc);
            });
            return res;
        }
        catch (Exception e)
        {
            Debug.WriteLine($"Error while execute get items: {request.AttributeAssemblyType}, error: {e.Message}");
            throw;
        }
    }
}