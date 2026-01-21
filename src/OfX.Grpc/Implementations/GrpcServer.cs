using System.Collections.Concurrent;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OfX.ApplicationModels;
using OfX.Exceptions;
using OfX.Extensions;
using OfX.Grpc.Exceptions;
using OfX.Implementations;
using OfX.Statics;

namespace OfX.Grpc.Implementations;

/// <summary>
/// gRPC server implementation that handles incoming OfX data requests.
/// </summary>
/// <param name="serviceProvider">The service provider for resolving handlers and pipelines.</param>
/// <remarks>
/// This server exposes two gRPC endpoints:
/// <list type="bullet">
///   <item><description><c>GetItems</c> - Fetches data for a specific attribute type and selector IDs</description></item>
///   <item><description><c>GetAttributes</c> - Returns the list of attribute types this server can handle (for discovery)</description></item>
/// </list>
/// </remarks>
public sealed class GrpcServer(IServiceProvider serviceProvider) : OfXTransportService.OfXTransportServiceBase
{
    private static readonly Lazy<ConcurrentDictionary<string, Type>> ReceivedPipelineTypes = new(() => []);
    private readonly ILogger<GrpcServer> _logger = serviceProvider.GetService<ILogger<GrpcServer>>();

    public override async Task<OfXItemsGrpcResponse> GetItems(GetOfXGrpcQuery request, ServerCallContext context)
    {
        // Create timeout CTS
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken);
        cts.CancelAfter(OfXStatics.DefaultRequestTimeout);
        var cancellationToken = cts.Token;

        try
        {
            var receivedPipelinesType = ReceivedPipelineTypes.Value
                .GetOrAdd(request.AttributeAssemblyType, static typeAssembly =>
                {
                    var attributeType = Type.GetType(typeAssembly);
                    if (attributeType is null)
                        throw new OfXGrpcExceptions.CannotDeserializeOfXAttributeType(typeAssembly);

                    if (!OfXStatics.AttributeMapHandlers.TryGetValue(attributeType, out var handlerType))
                        throw new OfXException.CannotFindHandlerForOfAttribute(attributeType);

                    var modelArg = handlerType.GetGenericArguments()[0];
                    return typeof(ReceivedPipelinesOrchestrator<,>).MakeGenericType(modelArg, attributeType);
                });

            // Use scoped service to prevent concurrent issues (e.g., with DbContext)
            using var scope = serviceProvider.CreateScope();
            var receivedPipelinesOrchestrator = (ReceivedPipelinesOrchestrator)scope.ServiceProvider
                .GetRequiredService(receivedPipelinesType)!;

            var headers = context.RequestHeaders.ToDictionary(k => k.Key, v => v.Value);

            var message = new OfXRequest([..request.SelectorIds], request.Expression);
            var response = await receivedPipelinesOrchestrator
                .ExecuteAsync(message, headers, cancellationToken);

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
        catch (OperationCanceledException) when (cts.IsCancellationRequested &&
                                                 !context.CancellationToken.IsCancellationRequested)
        {
            _logger?.LogWarning("Request timeout for gRPC GetItems: {AttributeType}", request.AttributeAssemblyType);
            throw new RpcException(new Status(StatusCode.DeadlineExceeded,
                $"Request timeout for {request.AttributeAssemblyType}"));
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Error while execute get items: {RequestAttributeAssemblyType}",
                request.AttributeAssemblyType);
            throw;
        }
    }

    public override Task<AttributeTypeResponse> GetAttributes(GetAttributesQuery request, ServerCallContext context)
    {
        var ofXConfigureStorage = OfXStatics.ModelConfigurations;
        var response = new AttributeTypeResponse();
        var attributeTypes = ofXConfigureStorage.Value
            .Select(a => a.OfXAttributeType.GetAssemblyName());
        response.AttributeTypes.AddRange(attributeTypes);
        return Task.FromResult(response);
    }
}