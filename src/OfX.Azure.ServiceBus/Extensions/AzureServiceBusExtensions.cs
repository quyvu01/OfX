using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.DependencyInjection;
using OfX.Abstractions.Transporting;
using OfX.Azure.ServiceBus.Abstractions;
using OfX.Azure.ServiceBus.ApplicationModels;
using OfX.Azure.ServiceBus.BackgroundServices;
using OfX.Azure.ServiceBus.Implementations;
using OfX.Azure.ServiceBus.Wrappers;
using OfX.Registries;

namespace OfX.Azure.ServiceBus.Extensions;

public static class AzureServiceBusExtensions
{
    public static void AddAzureServiceBus(this OfXRegister ofXRegister, Action<AzureServiceBusClientSetting> options)
    {
        var setting = new AzureServiceBusClientSetting();
        options.Invoke(setting);
        var connectionString = setting.ConnectionString;
        var serviceBusClientOptions = setting.ServiceBusClientOptions;
        var services = ofXRegister.ServiceCollection;
        services.AddSingleton(_ =>
        {
            var client = new ServiceBusClient(connectionString, serviceBusClientOptions);
            var adminClient = new ServiceBusAdministrationClient(connectionString);
            var clientWrapper = new AzureServiceBusClientWrapper(client, adminClient);
            return clientWrapper;
        });

        services.AddSingleton(typeof(IAzureServiceBusServer<,>), typeof(AzureServiceBusServer<,>));
        services.AddSingleton(typeof(OpenAzureServiceBusClient<>));
        services.AddSingleton<IRequestClient, AzureServiceBusClient>();
        services.AddHostedService<AzureServiceBusServerWorker>();
    }
}