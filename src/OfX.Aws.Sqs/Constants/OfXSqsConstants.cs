namespace OfX.Aws.Sqs.Constants;

internal static class OfXSqsConstants
{
    internal const int DefaultVisibilityTimeout = 30;
    internal const int DefaultWaitTimeSeconds = 20;
    internal const int DefaultMessageRetentionPeriod = 300; // 5 minutes for response queues
    internal const int MaxNumberOfMessages = 10;

    // SQS Queue Attribute Names
    internal const string AttributeVisibilityTimeout = "VisibilityTimeout";
    internal const string AttributeReceiveMessageWaitTimeSeconds = "ReceiveMessageWaitTimeSeconds";
    internal const string AttributeMessageRetentionPeriod = "MessageRetentionPeriod";

    // SQS Message Attribute Names
    internal const string MessageAttributeCorrelationId = "CorrelationId";
    internal const string MessageAttributeReplyTo = "ReplyTo";
    internal const string MessageAttributeType = "Type";
    internal const string MessageAttributeTraceparent = "traceparent";
    internal const string MessageAttributeTracestate = "tracestate";
    internal const string MessageAttributeAll = "All";
}