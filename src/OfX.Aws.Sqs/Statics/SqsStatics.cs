using Amazon;

namespace OfX.Aws.Sqs.Statics;

internal static class SqsStatics
{
    internal static string AwsAccessKeyId { get; set; }
    internal static string AwsSecretAccessKey { get; set; }
    internal static RegionEndpoint AwsRegion { get; set; }
    internal static string ServiceUrl { get; set; } // For LocalStack testing
}
