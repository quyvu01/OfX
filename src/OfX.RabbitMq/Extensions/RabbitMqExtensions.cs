using Microsoft.Extensions.DependencyInjection;
using OfX.Clients;
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
        ofXRegister.ServiceCollection.AddSingleton<IRabbitMqServer, RabbitMqServer>();
        ofXRegister.ServiceCollection.AddSingleton<IRabbitMqClient, RabbitMqClient>();
        ClientsInstaller.InstallRequestHandlers(ofXRegister.ServiceCollection, typeof(OfXRabbitMqClient<>));
        ofXRegister.ServiceCollection.AddScoped(typeof(IRabbitMqServerRpc<,>), typeof(RabbitMqServerRpc<,>));
        ofXRegister.ServiceCollection.AddHostedService<RabbitMqServerWorker>();
    }
}