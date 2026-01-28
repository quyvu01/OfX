using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OfX.ApplicationModels;
using OfX.Exceptions;
using OfX.Extensions;
using OfX.Grpc.Exceptions;
using OfX.Implementations;
using OfX.Statics;
using OfX.Telemetry;

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
    private const string TransportName = "grpc";

    public override async Task<OfXItemsGrpcResponse> GetItems(GetOfXGrpcQuery request, ServerCallContext context)
    {
        // Extract attribute name for telemetry
        var attributeName = request.AttributeAssemblyType?.Split(',')[0].Split('.').Last() ?? "Unknown";

        // Extract parent trace context from gRPC metadata
        ActivityContext parentContext = default;
        var traceparentHeader = context.RequestHeaders.FirstOrDefault(h => h.Key == "traceparent");
        if (traceparentHeader != null) ActivityContext.TryParse(traceparentHeader.Value, null, out parentContext);

        using var activity = OfXActivitySource.StartServerActivity(attributeName, parentContext);
        var stopwatch = Stopwatch.StartNew();

        // Create timeout CTS
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken);
        cts.CancelAfter(OfXStatics.DefaultRequestTimeout);
        var cancellationToken = cts.Token;

        try
        {
            activity?.SetMessagingTags(system: TransportName, destination: "grpc-endpoint",
                messageId: Activity.Current?.Id ?? Guid.NewGuid().ToString(),
                operation: "process");

            OfXDiagnostics.MessageReceive(TransportName, "grpc-endpoint", Activity.Current?.Id);

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

            string[] selectorIds = [..request.SelectorIds];
            var expressions = JsonSerializer.Deserialize<string[]>(request.Expression);

            var message = new OfXRequest(selectorIds, expressions);
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

            // Record success metrics
            stopwatch.Stop();
            var itemCount = response.Items?.Length ?? 0;

            OfXMetrics.RecordRequest(
                attributeName,
                TransportName,
                stopwatch.Elapsed.TotalMilliseconds,
                itemCount);

            activity?.SetOfXTags(expressions, selectorIds, itemCount);
            activity?.SetStatus(ActivityStatusCode.Ok);

            return res;
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested &&
                                                 !context.CancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();

            _logger?.LogWarning("Request timeout for gRPC GetItems: {AttributeType}", attributeName);

            OfXMetrics.RecordError(
                attributeName,
                TransportName,
                stopwatch.Elapsed.TotalMilliseconds,
                "TimeoutException");

            activity?.SetStatus(ActivityStatusCode.Error, "Request timeout");

            throw new RpcException(new Status(StatusCode.DeadlineExceeded,
                $"Request timeout for {attributeName}"));
        }
        catch (Exception e)
        {
            stopwatch.Stop();

            _logger?.LogError(e, "Error while execute get items: {RequestAttributeAssemblyType}", attributeName);

            OfXMetrics.RecordError(attributeName, TransportName,
                stopwatch.Elapsed.TotalMilliseconds, e.GetType().Name);

            OfXDiagnostics.RequestError(attributeName, TransportName, e, stopwatch.Elapsed);

            activity?.RecordException(e);
            activity?.SetStatus(ActivityStatusCode.Error, e.Message);

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