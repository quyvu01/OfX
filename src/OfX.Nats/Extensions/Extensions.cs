using OfX.Nats.Statics;

namespace OfX.Nats.Extensions;

internal static class Extensions
{
    internal static string GetNatsSubject(this Type type) =>
        string.IsNullOrEmpty(NatsStatics.TopicPrefix)
            ? $"ofx-{type.Namespace}-{type.Name}".ToLower()
            : $"{NatsStatics.TopicPrefix}-ofx-{type.Namespace}-{type.Name}".ToLower();
}