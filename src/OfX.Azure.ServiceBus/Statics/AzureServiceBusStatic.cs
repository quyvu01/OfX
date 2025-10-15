namespace OfX.Azure.ServiceBus.Statics;

internal static class AzureServiceBusStatic
{
    internal static string TopicPrefix { get; set; }
    internal static int MaxConcurrentSessions { get; set; } = 8;
}