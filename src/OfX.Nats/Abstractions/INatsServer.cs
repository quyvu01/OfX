using OfX.Attributes;

namespace OfX.Nats.Abstractions;

internal interface INatsServer
{
    Task StartAsync(CancellationToken cancellationToken = default);
}

internal interface INatsServer<TModel, TAttribute> : INatsServer
    where TAttribute : OfXAttribute where TModel : class;
