namespace OfX.Aws.Sqs.Extensions;

internal static class Extensions
{
    internal static string GetQueueName(this Type type) =>
        $"OfX-{type.Namespace}-{type.Name}".Replace(".", "-").ToLower();
}