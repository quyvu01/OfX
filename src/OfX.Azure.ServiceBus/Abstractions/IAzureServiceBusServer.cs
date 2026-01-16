using OfX.Attributes;

namespace OfX.Azure.ServiceBus.Abstractions;

internal interface IAzureServiceBusServer
{
    Task StartAsync(CancellationToken cancellationToken = default);
}

internal interface IAzureServiceBusServer<TModel, TAttribute> : IAzureServiceBusServer
    where TAttribute : OfXAttribute where TModel : class;
