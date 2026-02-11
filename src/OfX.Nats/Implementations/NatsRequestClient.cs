using System.Diagnostics;
using NATS.Client.Core;
using OfX.Abstractions;
using OfX.Abstractions.Transporting;
using OfX.Attributes;
using OfX.Exceptions;
using OfX.Extensions;
using OfX.Nats.Extensions;
using OfX.Nats.Wrappers;
using OfX.Responses;
using OfX.Configuration;
using OfX.Telemetry;

namespace OfX.Nats.Implementations;

internal sealed class NatsRequestClient(NatsClientWrapper natsClientWrapper) : IRequestClient
{
    private const string TransportName = "nats";

    public async Task<ItemsResponse<DataResponse>> RequestAsync<TAttribute>(
        RequestContext<TAttribute> requestContext) where TAttribute : OfXAttribute
    {
        // Start client-side activity for distributed tracing
        using var activity = OfXActivitySource.StartClientActivity<TAttribute>(TransportName);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Add trace context to headers for propagation
            var natsHeaders = new NatsHeaders();
            requestContext.Headers?.ForEach(h => natsHeaders.Add(h.Key, h.Value));

            // Propagate W3C trace context
            if (activity != null)
            {
                if (!string.IsNullOrEmpty(activity.Id))
                    natsHeaders.Add("traceparent", activity.Id);
                if (!string.IsNullOrEmpty(activity.TraceStateString))
                    natsHeaders.Add("tracestate", activity.TraceStateString);

                // Add OfX-specific tags
                activity.SetMessagingTags(
                    system: TransportName,
                    destination: typeof(TAttribute).GetNatsSubject(),
                    operation: "publish");

                activity.SetOfXTags(requestContext.Query.Expressions,
                    selectorIds: requestContext.Query.SelectorIds);
            }

            // Emit diagnostic event
            OfXDiagnostics.RequestStart(
                typeof(TAttribute).Name,
                TransportName,
                requestContext.Query.SelectorIds,
                requestContext.Query.Expressions);

            // Track active requests
            OfXMetrics.UpdateActiveRequests(1);

            var reply = await natsClientWrapper.NatsClient
                .RequestAsync<OfXQueryRequest<TAttribute>, Result>(
                    typeof(TAttribute).GetNatsSubject(),
                    requestContext.Query, natsHeaders,
                    replyOpts: new NatsSubOpts { Timeout = OfXStatics.DefaultRequestTimeout },
                    cancellationToken: requestContext.CancellationToken);

            var response = reply.Data;
            if (response is null) throw new OfXException.ReceivedException("Received null response from server");

            if (!response.IsSuccess)
            {
                throw response.Fault?.ToException()
                      ?? new OfXException.ReceivedException("Unknown error from server");
            }

            // Record success metrics
            stopwatch.Stop();
            var itemCount = response.Data?.Items?.Length ?? 0;

            OfXMetrics.RecordRequest(typeof(TAttribute).Name, TransportName, stopwatch.Elapsed.TotalMilliseconds,
                itemCount);

            OfXDiagnostics.RequestStop(typeof(TAttribute).Name, TransportName, itemCount, stopwatch.Elapsed);

            // Add item count to activity
            activity?.SetOfXTags(itemCount: itemCount);

            return response.Data;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // Record error metrics
            OfXMetrics.RecordError(typeof(TAttribute).Name, TransportName, stopwatch.Elapsed.TotalMilliseconds,
                ex.GetType().Name);

            OfXDiagnostics.RequestError(typeof(TAttribute).Name, TransportName, ex, stopwatch.Elapsed);

            // Record exception on activity
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