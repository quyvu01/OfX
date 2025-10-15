using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;

namespace OfX.Azure.ServiceBus.Wrappers;

internal record AzureServiceBusClientWrapper(
    ServiceBusClient ServiceBusClient,
    ServiceBusAdministrationClient ServiceBusAdministrationClient);