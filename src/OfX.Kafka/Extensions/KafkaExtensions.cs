using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OfX.Kafka.Abstractions;
using OfX.Kafka.ApplicationModels;
using OfX.Kafka.Implementations;
using OfX.Registries;

namespace OfX.Kafka.Extensions;

public static class KafkaExtensions
{
    public static void AddKafka(this OfXRegister ofXRegister, Action<KafkaConfigurator> options)
    {
        var config = new KafkaConfigurator();
        options.Invoke(config);
        ofXRegister.ServiceCollection.AddSingleton<IKafkaServer, KafkaServerOrchestrator>();
        ofXRegister.ServiceCollection.AddSingleton(typeof(IKafkaServer<>), typeof(KafkaServer<>));
        ofXRegister.ServiceCollection.AddSingleton(typeof(IKafkaClient), typeof(KafkaClient));
        Clients.ClientsInstaller.InstallMappableRequestHandlers(ofXRegister.ServiceCollection,
            typeof(IOfXKafkaClient<>), [..ofXRegister.OfXAttributeTypes]);
    }

    public static void StartKafkaListeningAsync(this IHost host)
    {
        var serviceProvider = host.Services;
        var server = serviceProvider.GetRequiredService<IKafkaServer>();
        Task.Factory.StartNew(() => server.StartAsync());
    }
}