using NATS.Client.Core;
using OfX.Nats.Statics;

namespace OfX.Nats.ApplicationModels;

public sealed class NatsClientSetting
{
    public void Url(string url) => NatsStatics.NatsUrl = url;
    public void TopicPrefix(string topicPrefix) => NatsStatics.NatsTopicPrefix = topicPrefix;
    public void NatsOpts(Action<NatsOpts>  options)
    {
        var opts = new NatsOpts();
        options.Invoke(opts);
        NatsStatics.NatsOpts = opts;
    }
}