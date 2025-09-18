using NATS.Client.Core;
using OfX.Nats.Statics;

namespace OfX.Nats.ApplicationModels;

public sealed class NatsClientSetting
{
    public void Url(string url) => NatsStatics.NatsUrl = url;
    public void TopicPrefix(string topicPrefix) => NatsStatics.NatsTopicPrefix = topicPrefix;

    public void NatsOpts(NatsOpts options = null) => NatsStatics.NatsOpts = options;
}