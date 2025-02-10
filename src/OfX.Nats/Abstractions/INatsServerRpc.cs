using OfX.Attributes;

namespace OfX.Nats.Abstractions;

internal interface INatsServerRpc
{
    Task StartAsync();
}

internal interface INatsServerRpc<TModel, TAttribute> : INatsServerRpc
    where TAttribute : OfXAttribute where TModel : class;