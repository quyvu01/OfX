using OfX.Aws.Sqs.Statics;

namespace OfX.Aws.Sqs.ApplicationModels;

public sealed class SqsCredential
{
    public void AccessKeyId(string accessKeyId) => SqsStatics.AwsAccessKeyId = accessKeyId;
    public void SecretAccessKey(string secretAccessKey) => SqsStatics.AwsSecretAccessKey = secretAccessKey;
    public void ServiceUrl(string serviceUrl) => SqsStatics.ServiceUrl = serviceUrl;
}
