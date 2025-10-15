using OfX.Azure.ServiceBus.Statics;

namespace OfX.Azure.ServiceBus.ApplicationModels;

public sealed class AzureServiceBusClientSetting
{
    public string ConnectionString { get; private set; }

    public void Host(string connectionString) => ConnectionString = connectionString;
    public void TopicPrefix(string topicPrefix) => AzureServiceBusStatic.TopicPrefix = topicPrefix;

    public void MaxConcurrentSessions(int maxConcurrentSessions)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxConcurrentSessions);
        AzureServiceBusStatic.MaxConcurrentSessions = maxConcurrentSessions;
    }
}