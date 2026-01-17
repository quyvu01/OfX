using Microsoft.Extensions.DependencyInjection;
using OfX.Abstractions.Transporting;
using OfX.Kafka.Abstractions;
using OfX.Kafka.ApplicationModels;
using OfX.Kafka.BackgroundServices;
using OfX.Kafka.Implementations;
using OfX.Registries;
using OfX.Statics;
using OfX.Supervision;

namespace OfX.Kafka.Extensions;

public static class KafkaExtensions
{
    public static void AddKafka(this OfXRegister ofXRegister, Action<KafkaConfigurator> options)
    {
        var config = new KafkaConfigurator();
        options.Invoke(config);
        var services = ofXRegister.ServiceCollection;
        services.AddSingleton(typeof(IKafkaServer<,>), typeof(KafkaServer<,>));
        services.AddSingleton<IRequestClient, KafkaClient>();

        // Register supervisor options: global > default
        var supervisorOptions = OfXStatics.SupervisorOptions ?? new SupervisorOptions();
        services.AddSingleton(supervisorOptions);

        // Use KafkaSupervisorWorker with supervisor pattern
        services.AddHostedService<KafkaSupervisorWorker>();
    }
}