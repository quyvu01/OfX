using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.DependencyInjection;
using OfX.Abstractions.Transporting;
using OfX.Azure.ServiceBus.Abstractions;
using OfX.Azure.ServiceBus.Configuration;
using OfX.Azure.ServiceBus.BackgroundServices;
using OfX.Azure.ServiceBus.Implementations;
using OfX.Azure.ServiceBus.Wrappers;
using OfX.Registries;
using OfX.Configuration;
using OfX.Supervision;

namespace OfX.Azure.ServiceBus.Extensions;

public static class AzureServiceBusExtensions
{
    public static void AddAzureServiceBus(this OfXConfigurator ofXRegister, Action<AzureServiceBusClientSetting> options)
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

        // Register supervisor options: global > default
        var supervisorOptions = OfXStatics.SupervisorOptions ?? new SupervisorOptions();
        services.AddSingleton(supervisorOptions);

        // Use AzureServiceBusSupervisorWorker with supervisor pattern
        services.AddHostedService<AzureServiceBusSupervisorWorker>();
    }
}