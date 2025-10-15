using OfX.Abstractions;
using OfX.Attributes;
using OfX.Azure.ServiceBus.Abstractions;
using OfX.Responses;

namespace OfX.Azure.ServiceBus.Implementations;

internal class AzureServiceBusHandler<TAttribute>(IAzureServiceBusClient<TAttribute> client)
    : IMappableRequestHandler<TAttribute> where TAttribute : OfXAttribute
{
    public Task<ItemsResponse<OfXDataResponse>> RequestAsync(RequestContext<TAttribute> requestContext) =>
        client.RequestAsync(requestContext);
}