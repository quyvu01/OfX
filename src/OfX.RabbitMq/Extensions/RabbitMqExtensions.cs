using Microsoft.Extensions.DependencyInjection;
using OfX.Abstractions.Transporting;
using OfX.RabbitMq.Abstractions;
using OfX.RabbitMq.ApplicationModels;
using OfX.RabbitMq.BackgroundServices;
using OfX.RabbitMq.Implementations;
using OfX.Registries;
using OfX.Statics;
using OfX.Supervision;

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

        // Register supervisor options: global > default
        var supervisorOptions = OfXStatics.SupervisorOptions ?? new SupervisorOptions();
        services.AddSingleton(supervisorOptions);

        // Use RabbitMqSupervisorWorker with supervisor pattern
        services.AddHostedService<RabbitMqSupervisorWorker>();
    }
}