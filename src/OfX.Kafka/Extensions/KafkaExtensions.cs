using Microsoft.Extensions.DependencyInjection;
using OfX.Kafka.Abstractions;
using OfX.Kafka.ApplicationModels;
using OfX.Kafka.BackgroundServices;
using OfX.Kafka.Implementations;
using OfX.Registries;

namespace OfX.Kafka.Extensions;

public static class KafkaExtensions
{
    public static void AddKafka(this OfXRegister ofXRegister, Action<KafkaConfigurator> options)
    {
        var config = new KafkaConfigurator();
        options.Invoke(config);
        ofXRegister.ServiceCollection.AddSingleton(typeof(IKafkaServer<,>), typeof(KafkaServer<,>));
        ofXRegister.ServiceCollection.AddSingleton<IKafkaClient, KafkaClient>();
        Clients.ClientsInstaller.InstallMappableRequestHandlers(ofXRegister.ServiceCollection,
            typeof(IOfXKafkaClient<>), [..ofXRegister.OfXAttributeTypes]);
        ofXRegister.ServiceCollection.AddHostedService<KafkaServerWorker>();
    }
}