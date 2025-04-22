namespace OfX.Nats.Statics;

internal static class NatsStatics
{
    internal static string NatsUrl { get; set; }
    internal static string NatsTopicPrefix { get; set; }
    internal static TimeSpan DefaultRequestTimeout { get; set; } = TimeSpan.FromSeconds(5);
    internal static TimeSpan DefaultConnectTimeout { get; set; } = TimeSpan.FromSeconds(2);
    internal static TimeSpan DefaultCommandTimeout { get; set; } = TimeSpan.FromSeconds(5);
}