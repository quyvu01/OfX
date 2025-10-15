using OfX.Abstractions;
using OfX.Attributes;
using OfX.Responses;

namespace OfX.Azure.ServiceBus.Abstractions;

internal interface IAzureServiceBusClient<TAttribute> where TAttribute : OfXAttribute
{
    Task<ItemsResponse<OfXDataResponse>> RequestAsync(RequestContext<TAttribute> requestContext);
}