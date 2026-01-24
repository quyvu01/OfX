using System.Diagnostics;
using OfX.Attributes;
using OfX.Statics;

namespace OfX.Telemetry;

/// <summary>
/// Provides OpenTelemetry-compatible distributed tracing support for OfX framework.
/// </summary>
/// <remarks>
/// This class creates Activity instances that are compatible with OpenTelemetry and can be
/// exported to tracing backends like Jaeger, Zipkin, or Application Insights.
///
/// To enable tracing, configure OpenTelemetry in your application:
/// <code>
/// services.AddOpenTelemetry()
///     .WithTracing(builder => builder
///         .AddSource("OfX")
///         .AddOtlpExporter());
/// </code>
///
/// When no listeners are configured, activity creation returns null with zero allocation.
/// </remarks>
public static class OfXActivitySource
{
    private static readonly ActivitySource Source = new(Constants.Source, Constants.Version);

    /// <summary>
    /// Starts a client-side activity for an OfX request.
    /// </summary>
    /// <typeparam name="TAttribute">The OfX attribute type being requested.</typeparam>
    /// <param name="transport">The transport mechanism (e.g., "kafka", "grpc", "rabbitmq").</param>
    /// <param name="operationName">Optional custom operation name. Defaults to "OfX.Request".</param>
    /// <returns>An Activity if tracing is enabled, otherwise null.</returns>
    /// <remarks>
    /// Client activities represent outbound requests from the application to OfX servers.
    /// They should be disposed when the request completes.
    /// </remarks>
    public static Activity StartClientActivity<TAttribute>(string transport, string operationName = null)
        where TAttribute : OfXAttribute
    {
        var activity = Source.StartActivity(operationName ?? Constants.Telemetry.OperationRequest, ActivityKind.Client);
        if (activity == null) return null;

        // OpenTelemetry semantic conventions
        activity.SetTag(Constants.Telemetry.TagOfXAttribute, typeof(TAttribute).Name);
        activity.SetTag(Constants.Telemetry.TagOfXTransport, transport);
        activity.SetTag(Constants.Telemetry.TagOfXVersion, Constants.Version);

        return activity;
    }

    /// <summary>
    /// Starts a server-side activity for processing an OfX request.
    /// </summary>
    /// <param name="attributeName">The name of the attribute being processed.</param>
    /// <param name="parentContext">The parent activity context from the incoming request.</param>
    /// <param name="operationName">Optional custom operation name. Defaults to "OfX.Process".</param>
    /// <returns>An Activity if tracing is enabled, otherwise null.</returns>
    /// <remarks>
    /// Server activities represent the processing of incoming requests.
    /// The parentContext should be extracted from the incoming message headers.
    /// </remarks>
    public static Activity StartServerActivity(string attributeName, ActivityContext parentContext = default,
        string operationName = null)
    {
        var activity = Source.StartActivity(
            operationName ?? Constants.Telemetry.OperationProcess,
            ActivityKind.Server,
            parentContext);

        if (activity == null) return null;

        activity.SetTag(Constants.Telemetry.TagOfXAttribute, attributeName);
        activity.SetTag(Constants.Telemetry.TagOfXVersion, Constants.Version);

        return activity;
    }

    /// <summary>
    /// Starts an internal activity for OfX operations (e.g., expression parsing, projection).
    /// </summary>
    /// <param name="operationName">The name of the operation.</param>
    /// <param name="kind">The activity kind. Defaults to Internal.</param>
    /// <returns>An Activity if tracing is enabled, otherwise null.</returns>
    public static Activity StartInternalActivity(string operationName, ActivityKind kind = ActivityKind.Internal)
    {
        return Source.StartActivity(operationName, kind);
    }

    /// <summary>
    /// Starts a database activity for query operations.
    /// </summary>
    /// <typeparam name="TAttribute">The OfX attribute type.</typeparam>
    /// <param name="dbSystem">The database system (e.g., "efcore", "mongodb").</param>
    /// <param name="operationName">Optional operation name. Defaults to "ofx.db.query".</param>
    /// <returns>An Activity if tracing is enabled, otherwise null.</returns>
    public static Activity StartDatabaseActivity<TAttribute>(string dbSystem, string operationName = null)
        where TAttribute : OfXAttribute
    {
        var activity = Source.StartActivity(operationName ?? "ofx.db.query", ActivityKind.Client);

        if (activity == null) return null;

        activity.SetTag(Constants.Telemetry.TagOfXAttribute, typeof(TAttribute).Name);
        activity.SetTag(Constants.Telemetry.TagOfXVersion, Constants.Version);
        activity.SetTag(Constants.Telemetry.TagDbSystem, dbSystem);

        return activity;
    }

    /// <param name="activity">The activity to record the exception on.</param>
    extension(Activity activity)
    {
        /// <summary>
        /// Records an exception on the current activity.
        /// </summary>
        /// <param name="exception">The exception to record.</param>
        /// <remarks>
        /// This follows OpenTelemetry semantic conventions for exception events.
        /// </remarks>
        public void RecordException(Exception exception)
        {
            if (activity == null) return;

            var tags = new ActivityTagsCollection
            {
                { Constants.Telemetry.TagExceptionType, exception.GetType().FullName },
                { Constants.Telemetry.TagExceptionMessage, exception.Message },
                { Constants.Telemetry.TagExceptionStacktrace, exception.StackTrace }
            };

            activity.AddEvent(new ActivityEvent(Constants.Telemetry.EventException, DateTimeOffset.UtcNow, tags));
            activity.SetStatus(ActivityStatusCode.Error, exception.Message);
        }

        /// <summary>
        /// Sets the status of an activity.
        /// </summary>
        /// <param name="code">The status code.</param>
        /// <param name="description">Optional status description.</param>
        public void SetStatus(ActivityStatusCode code, string description = null)
        {
            if (activity == null) return;

            activity.SetStatus(code);
            if (description != null) activity.SetTag(Constants.Telemetry.TagStatusDescription, description);
        }

        /// <summary>
        /// Adds messaging-specific tags to an activity.
        /// </summary>
        /// <param name="system">The messaging system (e.g., "kafka", "rabbitmq").</param>
        /// <param name="destination">The queue/topic name.</param>
        /// <param name="messageId">The message ID.</param>
        /// <param name="operation">The operation (e.g., "send", "receive", "process").</param>
        public void SetMessagingTags(string system,
            string destination = null,
            string messageId = null,
            string operation = null)
        {
            if (activity == null) return;

            activity.SetTag(Constants.Telemetry.TagMessagingSystem, system);
            if (destination != null) activity.SetTag(Constants.Telemetry.TagMessagingDestination, destination);
            if (messageId != null) activity.SetTag(Constants.Telemetry.TagMessagingMessageId, messageId);
            if (operation != null) activity.SetTag(Constants.Telemetry.TagMessagingOperation, operation);
        }

        /// <summary>
        /// Adds database-specific tags to an activity.
        /// </summary>
        /// <param name="dbSystem">The database system (e.g., "postgresql", "mongodb", "efcore").</param>
        /// <param name="dbName">The database name.</param>
        /// <param name="statement">The database statement (optional, be careful with PII).</param>
        /// <param name="collection">The collection/table name (for MongoDB).</param>
        /// <param name="operation">The database operation (e.g., "query", "find", "insert").</param>
        public void SetDatabaseTags(string dbSystem,
            string dbName = null,
            string statement = null,
            string collection = null,
            string operation = null)
        {
            if (activity == null) return;

            activity.SetTag(Constants.Telemetry.TagDbSystem, dbSystem);
            if (dbName != null) activity.SetTag(Constants.Telemetry.TagDbName, dbName);
            if (statement != null) activity.SetTag(Constants.Telemetry.TagDbStatement, statement);
            if (collection != null) activity.SetTag(Constants.Telemetry.TagDbCollection, collection);
            if (operation != null) activity.SetTag(Constants.Telemetry.TagDbOperation, operation);
        }

        /// <summary>
        /// Adds OfX-specific tags to an activity.
        /// </summary>
        /// <param name="expression">The OfX expression being evaluated.</param>
        /// <param name="selectorIds">The selector IDs being queried.</param>
        /// <param name="itemCount">The number of items returned.</param>
        public void SetOfXTags(string expression = null, string[] selectorIds = null, int? itemCount = null)
        {
            if (activity is null) return;

            activity.SetTag(Constants.Telemetry.TagOfXExpression, expression ?? "[null]");
            if (selectorIds is { Length: > 0 })
            {
                activity.SetTag(Constants.Telemetry.TagOfXSelectorCount, selectorIds.Length);
                // Only log first few IDs to avoid huge tags
                activity.SetTag(Constants.Telemetry.TagOfXSelectorIds, string.Join(",", selectorIds.Take(5)));
            }

            if (itemCount.HasValue) activity.SetTag(Constants.Telemetry.TagOfXItemCount, itemCount.Value);
        }
    }
}