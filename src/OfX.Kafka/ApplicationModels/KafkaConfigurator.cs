using OfX.Kafka.Statics;

namespace OfX.Kafka.ApplicationModels;

public sealed class KafkaConfigurator
{
    public void Host(string host) => KafkaStatics.KafkaHost = host;

    public void Ssl(Action<KafkaSslOptions> kafkaSslOptions)
    {
        var  options = new KafkaSslOptions();
        kafkaSslOptions(options);
        KafkaStatics.KafkaSslOptions = options;
    }
}