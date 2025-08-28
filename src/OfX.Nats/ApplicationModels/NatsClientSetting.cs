using NATS.Client.Core;
using OfX.Nats.Statics;

namespace OfX.Nats.ApplicationModels;

public sealed class NatsClientSetting
{
    private const string DefaultUrl = "nats://localhost:4222";
    public void Url(string url) => NatsStatics.NatsUrl = url ?? DefaultUrl;
    public void TopicPrefix(string topicPrefix) => NatsStatics.NatsTopicPrefix = topicPrefix;

    public void NatsOpts(NatsOpts options = null) => NatsStatics.NatsOpts = options;
}