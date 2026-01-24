#nullable enable
using System.Diagnostics;
using System.Diagnostics.Metrics;
using OfX.Statics;

namespace OfX.Telemetry;

/// <summary>
/// Provides OpenTelemetry-compatible metrics for OfX framework operations.
/// </summary>
/// <remarks>
/// This class exposes metrics that can be collected by OpenTelemetry and exported to
/// monitoring systems like Prometheus, Grafana, or Application Insights.
///
/// To enable metrics, configure OpenTelemetry in your application:
/// <code>
/// services.AddOpenTelemetry()
///     .WithMetrics(builder => builder
///         .AddMeter("OfX")
///         .AddPrometheusExporter());
/// </code>
///
/// Metrics are lazy-initialized and have zero overhead when not observed.
/// </remarks>
public static class OfXMetrics
{
    private static readonly Meter Meter = new(Constants.Source, Constants.Version);

    #region Counters

    /// <summary>
    /// Total number of OfX requests initiated.
    /// </summary>
    /// <remarks>
    /// Recommended dimensions: attribute, transport, status
    /// </remarks>
    public static readonly Counter<long> RequestCount =
        Meter.CreateCounter<long>(
            Constants.Telemetry.MetricRequestCount,
            description: Constants.Telemetry.DescriptionRequestCount);

    /// <summary>
    /// Total number of OfX request errors.
    /// </summary>
    /// <remarks>
    /// Recommended dimensions: attribute, transport, error_type
    /// </remarks>
    public static readonly Counter<long> ErrorCount =
        Meter.CreateCounter<long>(
            Constants.Telemetry.MetricRequestErrors,
            description: Constants.Telemetry.DescriptionRequestErrors);

    /// <summary>
    /// Total number of items returned across all requests.
    /// </summary>
    /// <remarks>
    /// Recommended dimensions: attribute, transport
    /// </remarks>
    public static readonly Counter<long> ItemsReturned =
        Meter.CreateCounter<long>(
            Constants.Telemetry.MetricItemsReturned,
            description: Constants.Telemetry.DescriptionItemsReturned);

    /// <summary>
    /// Total number of messages sent to messaging systems.
    /// </summary>
    /// <remarks>
    /// Recommended dimensions: transport, destination
    /// </remarks>
    public static readonly Counter<long> MessagesSent =
        Meter.CreateCounter<long>(
            Constants.Telemetry.MetricMessagesSent,
            description: Constants.Telemetry.DescriptionMessagesSent);

    /// <summary>
    /// Total number of messages received from messaging systems.
    /// </summary>
    /// <remarks>
    /// Recommended dimensions: transport, source
    /// </remarks>
    public static readonly Counter<long> MessagesReceived =
        Meter.CreateCounter<long>(
            Constants.Telemetry.MetricMessagesReceived,
            description: Constants.Telemetry.DescriptionMessagesReceived);

    #endregion

    #region Histograms

    /// <summary>
    /// Duration of OfX requests in milliseconds.
    /// </summary>
    /// <remarks>
    /// Recommended dimensions: attribute, transport, status
    /// </remarks>
    public static readonly Histogram<double> RequestDuration =
        Meter.CreateHistogram<double>(
            Constants.Telemetry.MetricRequestDuration,
            unit: Constants.Telemetry.UnitMilliseconds,
            description: Constants.Telemetry.DescriptionRequestDuration);

    /// <summary>
    /// Number of items returned per request.
    /// </summary>
    /// <remarks>
    /// Recommended dimensions: attribute, transport
    /// </remarks>
    public static readonly Histogram<int> ItemsPerRequest =
        Meter.CreateHistogram<int>(
            Constants.Telemetry.MetricItemsPerRequest,
            description: Constants.Telemetry.DescriptionItemsPerRequest);

    /// <summary>
    /// Size of messages in bytes.
    /// </summary>
    /// <remarks>
    /// Recommended dimensions: transport, direction (send/receive)
    /// </remarks>
    public static readonly Histogram<long> MessageSize =
        Meter.CreateHistogram<long>(
            Constants.Telemetry.MetricMessageSize,
            unit: Constants.Telemetry.UnitBytes,
            description: Constants.Telemetry.DescriptionMessageSize);

    /// <summary>
    /// Duration of database queries in milliseconds.
    /// </summary>
    /// <remarks>
    /// Recommended dimensions: db_system, operation
    /// </remarks>
    public static readonly Histogram<double> DatabaseQueryDuration =
        Meter.CreateHistogram<double>(
            Constants.Telemetry.MetricDatabaseQueryDuration,
            unit: Constants.Telemetry.UnitMilliseconds,
            description: Constants.Telemetry.DescriptionDatabaseQueryDuration);

    /// <summary>
    /// Duration of expression parsing in milliseconds.
    /// </summary>
    /// <remarks>
    /// Recommended dimensions: complexity (simple/medium/complex)
    /// </remarks>
    public static readonly Histogram<double> ExpressionParsingDuration =
        Meter.CreateHistogram<double>(
            Constants.Telemetry.MetricExpressionParsingDuration,
            unit: Constants.Telemetry.UnitMilliseconds,
            description: Constants.Telemetry.DescriptionExpressionParsingDuration);

    #endregion

    #region Gauges (Observable)

    /// <summary>
    /// Current number of active OfX requests.
    /// </summary>
    /// <remarks>
    /// This is an observable gauge that tracks in-flight requests.
    /// To use, call UpdateActiveRequests() when requests start/complete.
    /// </remarks>
    private static long _activeRequests;

    public static readonly ObservableGauge<long> ActiveRequests =
        Meter.CreateObservableGauge(
            Constants.Telemetry.MetricRequestsActive,
            () => Interlocked.Read(ref _activeRequests),
            description: Constants.Telemetry.DescriptionRequestsActive);

    /// <summary>
    /// Updates the active requests count.
    /// </summary>
    /// <param name="delta">+1 when starting a request, -1 when completing.</param>
    public static void UpdateActiveRequests(int delta)
    {
        Interlocked.Add(ref _activeRequests, delta);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Records a successful request with common tags.
    /// </summary>
    /// <param name="attributeName">The OfX attribute name.</param>
    /// <param name="transport">The transport mechanism.</param>
    /// <param name="durationMs">The request duration in milliseconds.</param>
    /// <param name="itemCount">The number of items returned.</param>
    public static void RecordRequest(string attributeName, string transport, double durationMs, int itemCount)
    {
        var tags = new TagList
        {
            { Constants.Telemetry.LabelOfXAttribute, attributeName },
            { Constants.Telemetry.LabelOfXTransport, transport },
            { Constants.Telemetry.LabelOfXStatus, Constants.Telemetry.StatusSuccess }
        };

        RequestCount.Add(1, tags);
        RequestDuration.Record(durationMs, tags);
        ItemsPerRequest.Record(itemCount, tags);
        ItemsReturned.Add(itemCount, tags);
    }

    /// <summary>
    /// Records a failed request with common tags.
    /// </summary>
    /// <param name="attributeName">The OfX attribute name.</param>
    /// <param name="transport">The transport mechanism.</param>
    /// <param name="durationMs">The request duration in milliseconds.</param>
    /// <param name="errorType">The type of error that occurred.</param>
    public static void RecordError(
        string attributeName,
        string transport,
        double durationMs,
        string errorType)
    {
        var tags = new TagList
        {
            { Constants.Telemetry.LabelOfXAttribute, attributeName },
            { Constants.Telemetry.LabelOfXTransport, transport },
            { Constants.Telemetry.LabelOfXStatus, Constants.Telemetry.StatusError },
            { Constants.Telemetry.LabelOfXErrorType, errorType }
        };

        RequestCount.Add(1, tags);
        ErrorCount.Add(1, tags);
        RequestDuration.Record(durationMs, tags);
    }

    /// <summary>
    /// Records a successful database query.
    /// </summary>
    /// <param name="attributeName">The OfX attribute name.</param>
    /// <param name="dbSystem">The database system (e.g., "efcore", "mongodb").</param>
    /// <param name="durationMs">The query duration in milliseconds.</param>
    /// <param name="itemCount">The number of items returned.</param>
    public static void RecordDatabaseQuery(string attributeName, string dbSystem, double durationMs, int itemCount)
    {
        var tags = new TagList
        {
            { Constants.Telemetry.LabelOfXAttribute, attributeName },
            { Constants.Telemetry.LabelOfXDbSystem, dbSystem },
            { Constants.Telemetry.LabelOfXStatus, Constants.Telemetry.StatusSuccess }
        };

        DatabaseQueryDuration.Record(durationMs, tags);
        ItemsReturned.Add(itemCount, tags);
    }

    /// <summary>
    /// Records a failed database query.
    /// </summary>
    /// <param name="attributeName">The OfX attribute name.</param>
    /// <param name="dbSystem">The database system (e.g., "efcore", "mongodb").</param>
    /// <param name="durationMs">The query duration in milliseconds.</param>
    /// <param name="errorType">The type of error that occurred.</param>
    public static void RecordDatabaseError(string attributeName, string dbSystem, double durationMs, string errorType)
    {
        var tags = new TagList
        {
            { Constants.Telemetry.LabelOfXAttribute, attributeName },
            { Constants.Telemetry.LabelOfXDbSystem, dbSystem },
            { Constants.Telemetry.LabelOfXStatus, Constants.Telemetry.StatusError },
            { Constants.Telemetry.LabelOfXErrorType, errorType }
        };

        DatabaseQueryDuration.Record(durationMs, tags);
        ErrorCount.Add(1, tags);
    }

    /// <summary>
    /// Records a message send operation.
    /// </summary>
    /// <param name="transport">The transport mechanism.</param>
    /// <param name="destination">The destination (queue/topic name).</param>
    /// <param name="sizeBytes">The message size in bytes.</param>
    public static void RecordMessageSend(string transport, string destination, long sizeBytes)
    {
        var tags = new TagList
        {
            { Constants.Telemetry.LabelOfXTransport, transport },
            { Constants.Telemetry.LabelOfXDestination, destination },
            { Constants.Telemetry.LabelOfXDirection, Constants.Telemetry.DirectionSend }
        };

        MessagesSent.Add(1, tags);
        MessageSize.Record(sizeBytes, tags);
    }

    /// <summary>
    /// Records a message receive operation.
    /// </summary>
    /// <param name="transport">The transport mechanism.</param>
    /// <param name="source">The source (queue/topic name).</param>
    /// <param name="sizeBytes">The message size in bytes.</param>
    public static void RecordMessageReceive(string transport, string source, long sizeBytes)
    {
        var tags = new TagList
        {
            { Constants.Telemetry.LabelOfXTransport, transport },
            { Constants.Telemetry.LabelOfXSource, source },
            { Constants.Telemetry.LabelOfXDirection, Constants.Telemetry.DirectionReceive }
        };

        MessagesReceived.Add(1, tags);
        MessageSize.Record(sizeBytes, tags);
    }

    /// <summary>
    /// Records a database query operation.
    /// </summary>
    /// <param name="dbSystem">The database system (e.g., "postgresql", "mongodb").</param>
    /// <param name="operation">The operation type (e.g., "select", "insert").</param>
    /// <param name="durationMs">The query duration in milliseconds.</param>
    public static void RecordDatabaseQuery(string dbSystem, string operation, double durationMs)
    {
        var tags = new TagList
        {
            { Constants.Telemetry.LabelOfXDbSystem, dbSystem },
            { Constants.Telemetry.LabelOfXOperation, operation }
        };

        DatabaseQueryDuration.Record(durationMs, tags);
    }

    /// <summary>
    /// Records expression parsing operation.
    /// </summary>
    /// <param name="expression">The expression being parsed.</param>
    /// <param name="durationMs">The parsing duration in milliseconds.</param>
    public static void RecordExpressionParsing(string expression, double durationMs)
    {
        var complexity = expression.Length switch
        {
            < 20 => Constants.Telemetry.ComplexitySimple,
            < 100 => Constants.Telemetry.ComplexityMedium,
            _ => Constants.Telemetry.ComplexityComplex
        };

        var tags = new TagList { { Constants.Telemetry.LabelOfXComplexity, complexity } };
        ExpressionParsingDuration.Record(durationMs, tags);
    }

    #endregion
}