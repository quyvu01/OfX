using Microsoft.Extensions.DependencyInjection;
using NATS.Client;
using OfX.Nats.Abstractions;
using OfX.Nats.ApplicationModels;
using OfX.Nats.Implementations;
using OfX.Nats.Servers;
using OfX.Registries;

namespace OfX.Nats.Extensions;

public static class NatsExtensions
{
    public static void AddNats(this OfXRegister ofXRegister, Action<NatsClientRegister> options)
    {
        var newClientsRegister = new NatsClientRegister();
        options.Invoke(newClientsRegister);
        // Register NATS connection as a singleton
        ofXRegister.ServiceCollection.AddSingleton(_ =>
        {
            var connectionFactory = new ConnectionFactory();
            var natsOptions = ConnectionFactory.GetDefaultOptions();
            natsOptions.AllowReconnect = true;
            natsOptions.MaxReconnect = -1;
            natsOptions.ReconnectWait = 2000;
            natsOptions.Timeout = 5000;
            natsOptions.Url = newClientsRegister.NatsClient.NatsHost;
            natsOptions.User = newClientsRegister.NatsClient.NatsCredential.NatsUserName;
            natsOptions.Password = newClientsRegister.NatsClient.NatsCredential.NatsPassword;
            return connectionFactory.CreateConnection(natsOptions);
        });
        ClientsRegister(ofXRegister.ServiceCollection);
        Clients.ClientsInstaller.InstallMappableRequestHandlers(ofXRegister.ServiceCollection,
            typeof(IOfXNatsClient<>), [..ofXRegister.OfXAttributeTypes]);
    }

    private static void ClientsRegister(IServiceCollection serviceCollection) =>
        serviceCollection.AddScoped(typeof(INatsRequester<>), typeof(NatsRequester<>));

    public static void StartNatsServerAsync(this IServiceProvider serviceProvider)
    {
        var serverListening = new NatsServersListening(serviceProvider);
        serverListening.StartAsync();
    }
}