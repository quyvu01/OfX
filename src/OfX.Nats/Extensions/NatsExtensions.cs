using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NATS.Client;
using NATS.Net;
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
            var client = new NatsClient(newClientsRegister.NatsClientConfig.NatsHost);
            return client;
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
        _ = serverListening.StartAsync();
    }
}