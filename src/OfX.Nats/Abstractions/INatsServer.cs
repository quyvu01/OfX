using OfX.Attributes;

namespace OfX.Nats.Abstractions;

internal interface INatsServer
{
    Task StartAsync();
}

internal interface INatsServer<TModel, TAttribute> : INatsServer
    where TAttribute : OfXAttribute where TModel : class;