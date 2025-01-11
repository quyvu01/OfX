namespace OfX.Kafka.Extensions;

internal static class Extensions
{
    internal static string RoutingKey(this Type type) => $"OfX-{type.Namespace}:{type.Name}";
}