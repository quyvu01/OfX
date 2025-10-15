using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.DependencyInjection;
using OfX.Azure.ServiceBus.Abstractions;
using OfX.Azure.ServiceBus.ApplicationModels;
using OfX.Azure.ServiceBus.BackgroundServices;
using OfX.Azure.ServiceBus.Implementations;
using OfX.Azure.ServiceBus.Wrappers;
using OfX.Extensions;
using OfX.Registries;
using OfX.Wrappers;

namespace OfX.Azure.ServiceBus.Extensions;

public static class AzureServiceBusExtensions
{
    public static void AddAzureServiceBus(this OfXRegister ofXRegister, Action<AzureServiceBusClientSetting> options)
    {
        var setting = new AzureServiceBusClientSetting();
        options.Invoke(setting);
        var connectionString = setting.ConnectionString;
        var client = new ServiceBusClient(connectionString);
        var adminClient = new ServiceBusAdministrationClient(connectionString);
        var clientWrapper = new AzureServiceBusClientWrapper(client, adminClient);
        ofXRegister.ServiceCollection.AddSingleton(clientWrapper);

        ofXRegister.ServiceCollection.AddSingleton(typeof(IAzureServiceBusServer<,>), typeof(AzureServiceBusServer<,>));
        ofXRegister.ServiceCollection.AddHostedService<AzureServiceBusServerWorker>();
        ofXRegister.ServiceCollection.AddSingleton(typeof(IAzureServiceBusClient<>), typeof(AzureServiceBusClient<>));

        OfXForClientWrapped.Of(ofXRegister).InstallRequestHandlers(typeof(AzureServiceBusHandler<>));
    }
}