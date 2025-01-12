namespace OfX.Kafka.Extensions;

internal static class Extensions
{
    internal static string RequestTopic(this Type type) => $"ofx-request-topic-{type.FullName}".ToLower();
}