using Microsoft.Extensions.DependencyInjection;
using NATS.Net;
using OfX.Extensions;
using OfX.Nats.Abstractions;
using OfX.Nats.ApplicationModels;
using OfX.Nats.BackgroundServices;
using OfX.Nats.Implementations;
using OfX.Nats.Servers;
using OfX.Nats.Statics;
using OfX.Nats.Wrappers;
using OfX.Registries;
using OfX.Wrappers;

namespace OfX.Nats.Extensions;

public static class NatsExtensions
{
    public static void AddNats(this OfXRegister ofXRegister, Action<NatsClientSetting> options)
    {
        var newClientsRegister = new NatsClientSetting();
        options.Invoke(newClientsRegister);
        ofXRegister.ServiceCollection.AddSingleton(_ => NatsStatics.NatsOpts != null
            ? new NatsClientWrapper(new NatsClient(NatsStatics.NatsOpts))
            : new NatsClientWrapper(new NatsClient(NatsStatics.NatsUrl)));
        ofXRegister.ServiceCollection.AddSingleton(typeof(INatsServerRpc<,>), typeof(NatsServerRpc<,>));
        ofXRegister.ServiceCollection.AddHostedService<NatsServerWorker>();
        ofXRegister.ServiceCollection.AddTransient(typeof(INatsRequester<>), typeof(NatsRequester<>));
        OfXForClientWrapped.Of(ofXRegister).InstallRequestHandlers(typeof(OfXNatsClient<>));
    }
}