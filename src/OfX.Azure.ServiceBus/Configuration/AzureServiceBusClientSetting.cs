using Azure.Messaging.ServiceBus;
using OfX.Azure.ServiceBus.Statics;

namespace OfX.Azure.ServiceBus.Configuration;

public sealed class AzureServiceBusClientSetting
{
    public string ConnectionString { get; private set; }
    public ServiceBusClientOptions ServiceBusClientOptions { get; private set; }

    public void Host(string connectionString, Action<ServiceBusClientOptions> serviceBusClientOptions = null)
    {
        ConnectionString = connectionString;
        if (serviceBusClientOptions == null) return;
        var options = new ServiceBusClientOptions();
        serviceBusClientOptions.Invoke(options);
        ServiceBusClientOptions = options;
    }

    public void TopicPrefix(string topicPrefix) => AzureServiceBusStatic.TopicPrefix = topicPrefix;

    public void MaxConcurrentSessions(int maxConcurrentSessions)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxConcurrentSessions, 1);
        AzureServiceBusStatic.MaxConcurrentSessions = maxConcurrentSessions;
    }
}