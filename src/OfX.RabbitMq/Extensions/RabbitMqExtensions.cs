using Microsoft.Extensions.DependencyInjection;
using OfX.Extensions;
using OfX.RabbitMq.Abstractions;
using OfX.RabbitMq.ApplicationModels;
using OfX.RabbitMq.BackgroundServices;
using OfX.RabbitMq.Implementations;
using OfX.Registries;
using OfX.Wrappers;

namespace OfX.RabbitMq.Extensions;

public static class RabbitMqExtensions
{
    public static void AddRabbitMq(this OfXRegister ofXRegister, Action<RabbitMqConfigurator> options)
    {
        var config = new RabbitMqConfigurator();
        options.Invoke(config);
        var services = ofXRegister.ServiceCollection;
        services.AddSingleton<IRabbitMqServer, RabbitMqServer>();
        services.AddSingleton<IRabbitMqClient, RabbitMqClient>();
        services.AddHostedService<RabbitMqServerHostedService>();
        OfXForClientWrapped.Of(ofXRegister).InstallRequestHandlers(typeof(RabbitMqRequestHandler<>));
    }
}