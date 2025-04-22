using Microsoft.Extensions.DependencyInjection;
using NATS.Client.Core;
using NATS.Net;
using OfX.Clients;
using OfX.Nats.Abstractions;
using OfX.Nats.ApplicationModels;
using OfX.Nats.BackgroundServices;
using OfX.Nats.Implementations;
using OfX.Nats.Servers;
using OfX.Nats.Statics;
using OfX.Nats.Wrappers;
using OfX.Registries;

namespace OfX.Nats.Extensions;

public static class NatsExtensions
{
    public static void AddNats(this OfXRegister ofXRegister, Action<NatsClientSetting> options)
    {
        var newClientsRegister = new NatsClientSetting();
        options.Invoke(newClientsRegister);
        var opts = new NatsOpts
        {
            Url = NatsStatics.NatsUrl,
            RequestTimeout = NatsStatics.DefaultRequestTimeout,
            ConnectTimeout = NatsStatics.DefaultConnectTimeout,
            CommandTimeout = NatsStatics.DefaultCommandTimeout,
        };
        var client = new NatsClientWrapper(new NatsClient(opts));
        ofXRegister.ServiceCollection.AddSingleton(_ => client);
        ClientsRegister(ofXRegister.ServiceCollection);
        ClientsInstaller.InstallRequestHandlers(ofXRegister.ServiceCollection, typeof(OfXNatsClient<>));
        ofXRegister.ServiceCollection.AddSingleton(typeof(INatsServerRpc<,>), typeof(NatsServerRpc<,>));
        ofXRegister.ServiceCollection.AddHostedService<NatsServerWorker>();
    }

    private static void ClientsRegister(IServiceCollection serviceCollection) =>
        serviceCollection.AddScoped(typeof(INatsRequester<>), typeof(NatsRequester<>));
}