using OfX.Nats.Statics;

namespace OfX.Nats.Extensions;

internal static class Extensions
{
    public static string GetNatsSubject(this Type type) =>
        string.IsNullOrEmpty(NatsStatics.NatsTopicPrefix)
            ? $"OfX-{type.Namespace}:{type.Name}"
            : $"{NatsStatics.NatsTopicPrefix}-OfX-{type.Namespace}:{type.Name}";
}