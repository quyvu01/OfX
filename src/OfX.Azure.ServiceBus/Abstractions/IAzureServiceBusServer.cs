using OfX.Abstractions.Transporting;
using OfX.Attributes;

namespace OfX.Azure.ServiceBus.Abstractions;

internal interface IAzureServiceBusServer<TModel, TAttribute> : IRequestServer<TModel, TAttribute>
    where TAttribute : OfXAttribute where TModel : class;
