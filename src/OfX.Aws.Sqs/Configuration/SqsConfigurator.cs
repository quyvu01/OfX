using Amazon;
using OfX.Aws.Sqs.Statics;

namespace OfX.Aws.Sqs.Configuration;

public sealed class SqsConfigurator
{
    public void Region(RegionEndpoint region, Action<SqsCredential> configure = null)
    {
        SqsStatics.AwsRegion = region;
        var sqsCredential = new SqsCredential();
        configure?.Invoke(sqsCredential);
    }
}
