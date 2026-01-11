using Microsoft.Extensions.DependencyInjection;
using NATS.Net;
using OfX.Abstractions.Transporting;
using OfX.Nats.Abstractions;
using OfX.Nats.ApplicationModels;
using OfX.Nats.BackgroundServices;
using OfX.Nats.Implementations;
using OfX.Nats.Wrappers;
using OfX.Registries;

namespace OfX.Nats.Extensions;

public static class NatsExtensions
{
    public static void AddNats(this OfXRegister ofXRegister, Action<NatsClientSetting> options)
    {
        var natsSetting = new NatsClientSetting();
        options.Invoke(natsSetting);
        var services = ofXRegister.ServiceCollection;
        var defaultNatsUrl = natsSetting.DefaultNatsUrl;
        services.AddSingleton(_ =>
        {
            if (natsSetting.NatsOption is not { } natsOption)
                return new NatsClientWrapper(new NatsClient(natsSetting.NatsUrl ?? defaultNatsUrl));
            var natsUrl = natsOption.Url != natsSetting.DefaultNatsUrl ? natsOption.Url :
                natsSetting.NatsUrl != defaultNatsUrl ? natsSetting.NatsUrl : defaultNatsUrl;
            return new NatsClientWrapper(new NatsClient(natsOption with { Url = natsUrl }));
        });
        services.AddSingleton(typeof(INatsServer<,>), typeof(NatsServer<,>));
        services.AddHostedService<NatsServerWorker>();
        services.AddTransient<IRequestClient, NatsRequestClient>();
    }
}