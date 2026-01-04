using Microsoft.Extensions.DependencyInjection;
using OfX.Abstractions.Transporting;
using OfX.RabbitMq.Abstractions;
using OfX.RabbitMq.ApplicationModels;
using OfX.RabbitMq.BackgroundServices;
using OfX.RabbitMq.Implementations;
using OfX.Registries;

namespace OfX.RabbitMq.Extensions;

public static class RabbitMqExtensions
{
    public static void AddRabbitMq(this OfXRegister ofXRegister, Action<RabbitMqConfigurator> options)
    {
        var config = new RabbitMqConfigurator();
        options.Invoke(config);
        var services = ofXRegister.ServiceCollection;
        services.AddSingleton<IRabbitMqServer, RabbitMqServer>();
        services.AddSingleton<IRequestClient, RabbitMqRequestClient>();
        services.AddHostedService<RabbitMqServerHostedService>();
    }
}