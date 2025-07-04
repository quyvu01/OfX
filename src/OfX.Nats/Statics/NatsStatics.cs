using NATS.Client.Core;

namespace OfX.Nats.Statics;

internal static class NatsStatics
{
    internal static string NatsUrl { get; set; }
    internal static string NatsTopicPrefix { get; set; }
    internal static NatsOpts NatsOpts { get; set; }
}