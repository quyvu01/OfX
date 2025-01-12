namespace OfX.Nats.Extensions;

internal static class Extensions
{
    public static string GetNatsSubject(this Type type) => $"OfX-{type.Namespace}:{type.Name}";
}