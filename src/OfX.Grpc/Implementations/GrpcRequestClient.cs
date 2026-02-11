using System.Diagnostics;
using OfX.Abstractions;
using OfX.Abstractions.Transporting;
using OfX.Models;
using OfX.Attributes;
using OfX.Grpc.Delegates;
using OfX.Grpc.Internals;
using OfX.Responses;
using OfX.Telemetry;

namespace OfX.Grpc.Implementations;

/// <summary>
/// gRPC implementation of <see cref="IRequestClient"/> for sending OfX requests over gRPC.
/// </summary>
/// <param name="ofXResponseFunc">The function delegate for making gRPC calls based on attribute type.</param>
/// <remarks>
/// This client is automatically registered when <c>AddGrpcClients</c> is called and handles
/// the serialization and transport of OfX requests to remote gRPC servers.
/// </remarks>
public sealed class GrpcRequestClient(GetOfXResponseFunc ofXResponseFunc) : IRequestClient
{
    private const string TransportName = "grpc";

    public async Task<ItemsResponse<DataResponse>> RequestAsync<TAttribute>(
        RequestContext<TAttribute> requestContext) where TAttribute : OfXAttribute
    {
        // Start client-side activity for distributed tracing
        using var activity = OfXActivitySource.StartClientActivity<TAttribute>(TransportName);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Emit diagnostic event
            OfXDiagnostics.RequestStart(typeof(TAttribute).Name, TransportName, requestContext.Query.SelectorIds,
                requestContext.Query.Expressions);

            // Track active requests
            OfXMetrics.UpdateActiveRequests(1);

            // Note: W3C trace context propagation will be handled by gRPC infrastructure
            if (activity != null)
            {
                activity.SetMessagingTags(system: TransportName, destination: "grpc-server",
                    messageId: Activity.Current?.Id ?? Guid.NewGuid().ToString(), operation: "call");

                activity.SetOfXTags(requestContext.Query.Expressions, requestContext.Query.SelectorIds);
            }

            var func = ofXResponseFunc.Invoke(typeof(TAttribute));
            var result = await func.Invoke(
                new OfXRequest(requestContext.Query.SelectorIds, requestContext.Query.Expressions),
                new GrpcClientContext(requestContext.Headers, requestContext.CancellationToken));

            // Record success metrics
            stopwatch.Stop();
            var itemCount = result?.Items?.Length ?? 0;

            OfXMetrics.RecordRequest(typeof(TAttribute).Name, TransportName, stopwatch.Elapsed.TotalMilliseconds,
                itemCount);

            OfXDiagnostics.RequestStop(typeof(TAttribute).Name, TransportName, itemCount, stopwatch.Elapsed);

            activity?.SetOfXTags(itemCount: itemCount);
            activity?.SetStatus(ActivityStatusCode.Ok);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // Record error metrics
            OfXMetrics.RecordError(typeof(TAttribute).Name, TransportName, stopwatch.Elapsed.TotalMilliseconds,
                ex.GetType().Name);

            OfXDiagnostics.RequestError(typeof(TAttribute).Name, TransportName, ex, stopwatch.Elapsed);

            activity?.RecordException(ex);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

            throw;
        }
        finally
        {
            OfXMetrics.UpdateActiveRequests(-1);
        }
    }
}