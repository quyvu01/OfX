using OfX.Attributes;

namespace OfX.Nats.Abstractions;

internal interface INatsServerRpc
{
    void StartAsync();
}

internal interface INatsServerRpc<TModel, TAttribute> : INatsServerRpc
    where TAttribute : OfXAttribute where TModel : class;