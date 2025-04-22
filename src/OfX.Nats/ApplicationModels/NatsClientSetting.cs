using OfX.Nats.Statics;

namespace OfX.Nats.ApplicationModels;

public sealed class NatsClientSetting
{
    public void Url(string url) => NatsStatics.NatsUrl = url;
    public void TopicPrefix(string topicPrefix) => NatsStatics.NatsTopicPrefix = topicPrefix;

    public void DefaultRequestTimeout(int seconds = 5) => NatsStatics.DefaultRequestTimeout = TimeSpan.FromSeconds(seconds);
    public void DefaultConnectTimeout(int seconds = 2) => NatsStatics.DefaultConnectTimeout = TimeSpan.FromSeconds(seconds);
    public void DefaultCommandTimeout(int seconds = 5) => NatsStatics.DefaultCommandTimeout = TimeSpan.FromSeconds(seconds);
}