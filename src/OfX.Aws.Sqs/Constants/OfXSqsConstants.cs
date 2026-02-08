namespace OfX.Aws.Sqs.Constants;

internal static class OfXSqsConstants
{
    internal const string QueueNamePrefix = "ofx-rpc-queue";
    internal const int DefaultVisibilityTimeout = 30;
    internal const int DefaultWaitTimeSeconds = 20;
    internal const int DefaultMessageRetentionPeriod = 300; // 5 minutes for response queues
    internal const int MaxNumberOfMessages = 10;
}
