#nullable enable
using System.Diagnostics;
using OfX.Statics;

namespace OfX.Telemetry;

/// <summary>
/// Provides DiagnosticSource events for OfX framework operations.
/// </summary>
/// <remarks>
/// DiagnosticSource provides a lightweight event mechanism for telemetry that predates
/// OpenTelemetry Activity. It's useful for:
/// <list type="bullet">
///   <item><description>Custom telemetry consumers that don't use OpenTelemetry</description></item>
///   <item><description>Backward compatibility with existing monitoring solutions</description></item>
///   <item><description>High-performance event streaming scenarios</description></item>
/// </list>
///
/// To subscribe to events:
/// <code>
/// DiagnosticListener.AllListeners.Subscribe(listener =>
/// {
///     if (listener.Name == "OfX")
///     {
///         listener.Subscribe(evt =>
///         {
///             if (evt.Key == "OfX.Request.Start")
///             {
///                 // Handle event
///             }
///         });
///     }
/// });
/// </code>
/// </remarks>
public static class OfXDiagnostics
{
    private static readonly DiagnosticListener Listener = new(Constants.Source);

    #region Event Names

    public const string RequestStartEvent = Constants.Telemetry.EventRequestStart;
    public const string RequestStopEvent = Constants.Telemetry.EventRequestStop;
    public const string RequestErrorEvent = Constants.Telemetry.EventRequestError;
    public const string MessageSendEvent = Constants.Telemetry.EventMessageSend;
    public const string MessageReceiveEvent = Constants.Telemetry.EventMessageReceive;
    public const string DatabaseQueryStartEvent = Constants.Telemetry.EventDatabaseQueryStart;
    public const string DatabaseQueryStopEvent = Constants.Telemetry.EventDatabaseQueryStop;
    public const string ExpressionParseEvent = Constants.Telemetry.EventExpressionParse;
    public const string CacheLookupEvent = Constants.Telemetry.EventCacheLookup;

    #endregion

    #region Request Events

    /// <summary>
    /// Fires when an OfX request starts.
    /// </summary>
    /// <param name="attribute">The OfX attribute name.</param>
    /// <param name="transport">The transport mechanism.</param>
    /// <param name="selectorIds">The selector IDs being queried.</param>
    /// <param name="expressions">The OfX expressions.</param>
    public static void RequestStart(
        string attribute,
        string transport,
        string[]? selectorIds = null,
        string[]? expressions = null)
    {
        if (!Listener.IsEnabled(RequestStartEvent)) return;

        Listener.Write(RequestStartEvent, new
        {
            Attribute = attribute,
            Transport = transport,
            SelectorIds = selectorIds,
            Expressions = expressions,
            Timestamp = DateTimeOffset.UtcNow,
            OperationId = Activity.Current?.Id
        });
    }

    /// <summary>
    /// Fires when an OfX request completes successfully.
    /// </summary>
    /// <param name="attribute">The OfX attribute name.</param>
    /// <param name="transport">The transport mechanism.</param>
    /// <param name="itemCount">The number of items returned.</param>
    /// <param name="duration">The request duration.</param>
    public static void RequestStop(
        string attribute,
        string transport,
        int itemCount,
        TimeSpan duration)
    {
        if (!Listener.IsEnabled(RequestStopEvent)) return;

        Listener.Write(RequestStopEvent, new
        {
            Attribute = attribute,
            Transport = transport,
            ItemCount = itemCount,
            Duration = duration,
            DurationMs = duration.TotalMilliseconds,
            Timestamp = DateTimeOffset.UtcNow,
            OperationId = Activity.Current?.Id
        });
    }

    /// <summary>
    /// Fires when an OfX request fails.
    /// </summary>
    /// <param name="attribute">The OfX attribute name.</param>
    /// <param name="transport">The transport mechanism.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="duration">The request duration before failure.</param>
    public static void RequestError(
        string attribute,
        string transport,
        Exception exception,
        TimeSpan duration)
    {
        if (!Listener.IsEnabled(RequestErrorEvent)) return;

        Listener.Write(RequestErrorEvent, new
        {
            Attribute = attribute,
            Transport = transport,
            Exception = exception,
            ErrorType = exception.GetType().Name,
            ErrorMessage = exception.Message,
            Duration = duration,
            DurationMs = duration.TotalMilliseconds,
            Timestamp = DateTimeOffset.UtcNow,
            OperationId = Activity.Current?.Id
        });
    }

    #endregion

    #region Messaging Events

    /// <summary>
    /// Fires when a message is sent to a messaging system.
    /// </summary>
    /// <param name="transport">The transport mechanism.</param>
    /// <param name="destination">The destination (queue/topic name).</param>
    /// <param name="messageId">The message ID.</param>
    /// <param name="sizeBytes">The message size in bytes.</param>
    public static void MessageSend(
        string transport,
        string destination,
        string? messageId = null,
        long? sizeBytes = null)
    {
        if (!Listener.IsEnabled(MessageSendEvent)) return;

        Listener.Write(MessageSendEvent, new
        {
            Transport = transport,
            Destination = destination,
            MessageId = messageId,
            SizeBytes = sizeBytes,
            Timestamp = DateTimeOffset.UtcNow,
            OperationId = Activity.Current?.Id
        });
    }

    /// <summary>
    /// Fires when a message is received from a messaging system.
    /// </summary>
    /// <param name="transport">The transport mechanism.</param>
    /// <param name="source">The source (queue/topic name).</param>
    /// <param name="messageId">The message ID.</param>
    /// <param name="sizeBytes">The message size in bytes.</param>
    public static void MessageReceive(
        string transport,
        string source,
        string? messageId = null,
        long? sizeBytes = null)
    {
        if (!Listener.IsEnabled(MessageReceiveEvent)) return;

        Listener.Write(MessageReceiveEvent, new
        {
            Transport = transport,
            Source = source,
            MessageId = messageId,
            SizeBytes = sizeBytes,
            Timestamp = DateTimeOffset.UtcNow,
            OperationId = Activity.Current?.Id
        });
    }

    #endregion

    #region Database Events

    private const string DatabaseQueryErrorEvent = Constants.Telemetry.EventDatabaseQueryError;

    /// <summary>
    /// Fires when a database query starts.
    /// </summary>
    /// <param name="attribute">The OfX attribute name.</param>
    /// <param name="dbSystem">The database system (e.g., "efcore", "mongodb").</param>
    /// <param name="expressions">The OfX expressions being queried.</param>
    public static void DatabaseQueryStart(
        string attribute,
        string dbSystem,
        string[] expressions)
    {
        if (!Listener.IsEnabled(DatabaseQueryStartEvent)) return;

        Listener.Write(DatabaseQueryStartEvent, new
        {
            Attribute = attribute,
            DbSystem = dbSystem,
            Expressions = expressions,
            Timestamp = DateTimeOffset.UtcNow,
            OperationId = Activity.Current?.Id
        });
    }

    /// <summary>
    /// Fires when a database query completes successfully.
    /// </summary>
    /// <param name="attribute">The OfX attribute name.</param>
    /// <param name="dbSystem">The database system.</param>
    /// <param name="itemCount">The number of items returned.</param>
    /// <param name="duration">The query duration.</param>
    public static void DatabaseQueryStop(
        string attribute,
        string dbSystem,
        int itemCount,
        TimeSpan duration)
    {
        if (!Listener.IsEnabled(DatabaseQueryStopEvent)) return;

        Listener.Write(DatabaseQueryStopEvent, new
        {
            Attribute = attribute,
            DbSystem = dbSystem,
            ItemCount = itemCount,
            Duration = duration,
            DurationMs = duration.TotalMilliseconds,
            Timestamp = DateTimeOffset.UtcNow,
            OperationId = Activity.Current?.Id
        });
    }

    /// <summary>
    /// Fires when a database query fails.
    /// </summary>
    /// <param name="attribute">The OfX attribute name.</param>
    /// <param name="dbSystem">The database system.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="duration">The query duration before failure.</param>
    public static void DatabaseQueryError(
        string attribute,
        string dbSystem,
        Exception exception,
        TimeSpan duration)
    {
        if (!Listener.IsEnabled(DatabaseQueryErrorEvent)) return;

        Listener.Write(DatabaseQueryErrorEvent, new
        {
            Attribute = attribute,
            DbSystem = dbSystem,
            Exception = exception,
            ErrorType = exception.GetType().Name,
            ErrorMessage = exception.Message,
            Duration = duration,
            DurationMs = duration.TotalMilliseconds,
            Timestamp = DateTimeOffset.UtcNow,
            OperationId = Activity.Current?.Id
        });
    }

    #endregion

    #region Expression Events

    /// <summary>
    /// Fires when an expression is parsed.
    /// </summary>
    /// <param name="expression">The expression being parsed.</param>
    /// <param name="duration">The parsing duration.</param>
    /// <param name="success">Whether parsing succeeded.</param>
    public static void ExpressionParse(
        string expression,
        TimeSpan duration,
        bool success = true)
    {
        if (!Listener.IsEnabled(ExpressionParseEvent)) return;

        Listener.Write(ExpressionParseEvent, new
        {
            Expression = expression,
            ExpressionLength = expression.Length,
            Duration = duration,
            DurationMs = duration.TotalMilliseconds,
            Success = success,
            Timestamp = DateTimeOffset.UtcNow,
            OperationId = Activity.Current?.Id
        });
    }

    #endregion

    #region Cache Events

    /// <summary>
    /// Fires when a cache lookup occurs.
    /// </summary>
    /// <param name="cacheType">The type of cache.</param>
    /// <param name="key">The cache key.</param>
    /// <param name="hit">Whether the lookup was a hit or miss.</param>
    public static void CacheLookup(
        string cacheType,
        string key,
        bool hit)
    {
        if (!Listener.IsEnabled(CacheLookupEvent)) return;

        Listener.Write(CacheLookupEvent, new
        {
            CacheType = cacheType,
            Key = key,
            Hit = hit,
            Timestamp = DateTimeOffset.UtcNow,
            OperationId = Activity.Current?.Id
        });
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Checks if any diagnostic listener is enabled for the specified event.
    /// </summary>
    /// <param name="eventName">The event name.</param>
    /// <returns>True if enabled, otherwise false.</returns>
    public static bool IsEnabled(string eventName) => Listener.IsEnabled(eventName);

    /// <summary>
    /// Writes a custom diagnostic event.
    /// </summary>
    /// <param name="eventName">The event name.</param>
    /// <param name="data">The event data.</param>
    public static void Write(string eventName, object data)
    {
        if (!Listener.IsEnabled(eventName)) return;
        Listener.Write(eventName, data);
    }

    #endregion
}
