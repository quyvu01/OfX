namespace OfX.Statics;

public static class Constants
{
    public static readonly string Version = PackageInfo.Version;
    public const string Source = "OfX";

    /// <summary>
    /// OpenTelemetry and diagnostic event names and tag keys used across OfX telemetry.
    /// Following MassTransit-style naming conventions with 'ofx.' prefix for consistency.
    /// </summary>
    public static class Telemetry
    {
        // Activity/Span operation names (lowercase following OTel conventions)
        public const string OperationRequest = "ofx.request";
        public const string OperationProcess = "ofx.process";

        // OpenTelemetry semantic convention tag keys (activity tags) - ofx.* prefix
        public const string TagOfXAttribute = "ofx.attribute";
        public const string TagOfXTransport = "ofx.transport";
        public const string TagOfXVersion = "ofx.version";
        public const string TagOfXExpressions = "ofx.expressions";
        public const string TagOfXSelectorCount = "ofx.selector_count";
        public const string TagOfXSelectorIds = "ofx.selector_ids";
        public const string TagOfXItemCount = "ofx.item_count";
        public const string TagOfXStatus = "ofx.status";
        public const string TagOfXErrorType = "ofx.error_type";

        // Messaging tags (OpenTelemetry standard - keep messaging.* prefix)
        public const string TagMessagingSystem = "messaging.system";
        public const string TagMessagingDestination = "messaging.destination";
        public const string TagMessagingMessageId = "messaging.message_id";
        public const string TagMessagingOperation = "messaging.operation";

        // Database tags (OpenTelemetry standard - keep db.* prefix)
        public const string TagDbSystem = "db.system";
        public const string TagDbName = "db.name";
        public const string TagDbStatement = "db.statement";
        public const string TagDbCollection = "db.collection";
        public const string TagDbOperation = "db.operation";

        // Exception tags (OpenTelemetry standard - keep exception.* prefix)
        public const string TagExceptionType = "exception.type";
        public const string TagExceptionMessage = "exception.message";
        public const string TagExceptionStacktrace = "exception.stacktrace";

        // Status description (OpenTelemetry standard)
        public const string TagStatusDescription = "otel.status_description";

        // Metric label/dimension names (following MassTransit pattern: ofx.*)
        public const string LabelOfXAttribute = "ofx.attribute";
        public const string LabelOfXTransport = "ofx.transport";
        public const string LabelOfXStatus = "ofx.status";
        public const string LabelOfXErrorType = "ofx.error_type";
        public const string LabelOfXDestination = "ofx.destination";
        public const string LabelOfXSource = "ofx.source";
        public const string LabelOfXDirection = "ofx.direction";
        public const string LabelOfXDbSystem = "ofx.db_system";
        public const string LabelOfXOperation = "ofx.operation";
        public const string LabelOfXComplexity = "ofx.complexity";

        // Metric label values
        public const string StatusSuccess = "success";
        public const string StatusError = "error";
        public const string DirectionSend = "send";
        public const string DirectionReceive = "receive";

        // Complexity levels
        public const string ComplexitySimple = "simple";
        public const string ComplexityMedium = "medium";
        public const string ComplexityComplex = "complex";

        // DiagnosticSource event names (lowercase following OTel conventions)
        public const string EventRequestStart = "ofx.request.start";
        public const string EventRequestStop = "ofx.request.stop";
        public const string EventRequestError = "ofx.request.error";
        public const string EventMessageSend = "ofx.message.send";
        public const string EventMessageReceive = "ofx.message.receive";
        public const string EventDatabaseQueryStart = "ofx.database.query.start";
        public const string EventDatabaseQueryStop = "ofx.database.query.stop";
        public const string EventDatabaseQueryError = "ofx.database.query.error";
        public const string EventExpressionParse = "ofx.expression.parse";
        public const string EventCacheLookup = "ofx.cache.lookup";
        public const string EventException = "exception";

        // Metric names (following MassTransit pattern: ofx.*)
        public const string MetricRequestCount = "ofx.request.count";
        public const string MetricRequestErrors = "ofx.request.errors";
        public const string MetricItemsReturned = "ofx.items.returned";
        public const string MetricMessagesSent = "ofx.messages.sent";
        public const string MetricMessagesReceived = "ofx.messages.received";
        public const string MetricRequestDuration = "ofx.request.duration";
        public const string MetricItemsPerRequest = "ofx.items.per_request";
        public const string MetricMessageSize = "ofx.message.size";
        public const string MetricDatabaseQueryDuration = "ofx.database.query.duration";
        public const string MetricExpressionParsingDuration = "ofx.expression.parsing.duration";
        public const string MetricRequestsActive = "ofx.request.active";

        // Metric units
        public const string UnitMilliseconds = "ms";
        public const string UnitBytes = "bytes";

        // Metric descriptions
        public const string DescriptionRequestCount = "Total number of OfX requests";
        public const string DescriptionRequestErrors = "Total number of OfX request errors";
        public const string DescriptionItemsReturned = "Total number of items returned by OfX requests";
        public const string DescriptionMessagesSent = "Total number of messages sent";
        public const string DescriptionMessagesReceived = "Total number of messages received";
        public const string DescriptionRequestDuration = "Duration of OfX requests";
        public const string DescriptionItemsPerRequest = "Number of items returned per request";
        public const string DescriptionMessageSize = "Size of messages";
        public const string DescriptionDatabaseQueryDuration = "Duration of database queries";
        public const string DescriptionExpressionParsingDuration = "Duration of expression parsing";
        public const string DescriptionRequestsActive = "Current number of active OfX requests";
    }
}