using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OfX.RabbitMq.Abstractions;
using OfX.RabbitMq.ApplicationModels;
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
        Clients.ClientsInstaller.InstallMappableRequestHandlers(ofXRegister.ServiceCollection,
            typeof(IOfXRabbitMqClient<>), [..ofXRegister.OfXAttributeTypes]);
        ofXRegister.ServiceCollection.AddScoped(typeof(IRabbitMqServerRpc<,>), typeof(RabbitMqServerRpc<,>));
    }

    public static void StartRabbitMqListeningAsync(this IHost host)
    {
        var serviceProvider = host.Services;
        var server = serviceProvider.GetRequiredService<IRabbitMqServer>();
        Task.Factory.StartNew(() => server.ConsumeAsync());
    }
}