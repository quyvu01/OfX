using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NATS.Net;
using OfX.Nats.Abstractions;
using OfX.Nats.ApplicationModels;
using OfX.Nats.Implementations;
using OfX.Nats.Servers;
using OfX.Registries;

namespace OfX.Nats.Extensions;

public static class NatsExtensions
{
    public static void AddNats(this OfXRegister ofXRegister, Action<NatsClientHost> options)
    {
        var newClientsRegister = new NatsClientHost();
        options.Invoke(newClientsRegister);

        ofXRegister.ServiceCollection.AddSingleton(_ =>
        {
            var client = new NatsClient(newClientsRegister.NatsHost);
            return client;
        });
        ClientsRegister(ofXRegister.ServiceCollection);
        Clients.ClientsInstaller.InstallMappableRequestHandlers(ofXRegister.ServiceCollection,
            typeof(IOfXNatsClient<>), [..ofXRegister.OfXAttributeTypes]);
    }

    private static void ClientsRegister(IServiceCollection serviceCollection) =>
        serviceCollection.AddScoped(typeof(INatsRequester<>), typeof(NatsRequester<>));

    public static void StartNatsListeningAsync(this IHost host) =>
        NatsServersListening.StartAsync(host.Services);
}