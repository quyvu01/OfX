namespace OfX.RabbitMq.Extensions;

internal static class Extensions
{
    internal static string GetExchangeName(this Type type) => $"OfX-{type.Namespace}:{type.Name}";
}