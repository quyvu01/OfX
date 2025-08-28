using Microsoft.Extensions.DependencyInjection;
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
        options?.Invoke(newClientsRegister);
        ofXRegister.ServiceCollection.AddSingleton(_ => NatsStatics.NatsOpts != null
            ? new NatsClientWrapper(new NatsClient(NatsStatics.NatsOpts))
            : new NatsClientWrapper(new NatsClient(NatsStatics.NatsUrl)));
        ClientsInstaller.InstallRequestHandlers(ofXRegister.ServiceCollection, typeof(OfXNatsClient<>));
        ofXRegister.ServiceCollection.AddSingleton(typeof(INatsServerRpc<,>), typeof(NatsServerRpc<,>));
        ofXRegister.ServiceCollection.AddHostedService<NatsServerWorker>();
        ofXRegister.ServiceCollection.AddScoped(typeof(INatsRequester<>), typeof(NatsRequester<>));
    }
}