using Microsoft.Extensions.DependencyInjection;
using OfX.Abstractions.Agents;
using OfX.Extensions;
using OfX.RabbitMq.Abstractions;
using OfX.RabbitMq.ApplicationModels;
using OfX.RabbitMq.BackgroundServices;
using OfX.RabbitMq.Constants;
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

        services.AddSingleton<IConnectionContextFactory, RabbitMqConnectionContextFactory>();

        services.AddSingleton<IOfXBusControl>(sp =>
        {
            var bus = new OfXBus(sp.GetRequiredService<ConnectionContextSupervisor>());

            // Add receive endpoints
            var queueName = $"{OfXRabbitMqConstants.QueueNamePrefix}-{AppDomain.CurrentDomain.FriendlyName.ToLower()}";
            bus.AddReceiveEndpoint(queueName, async message =>
            {
                Console.WriteLine($"Received: {message}");
                await Task.CompletedTask;
            });

            return bus;
        });


        // ofXRegister.ServiceCollection.AddHostedService<RabbitMqServerHostedService>();
        OfXForClientWrapped.Of(ofXRegister).InstallRequestHandlers(typeof(RabbitMqRequestHandler<>));
    }
}