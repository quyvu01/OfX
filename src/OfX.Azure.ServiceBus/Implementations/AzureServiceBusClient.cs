using Microsoft.Extensions.DependencyInjection;
using OfX.Abstractions;
using OfX.Abstractions.Transporting;
using OfX.Attributes;
using OfX.Responses;

namespace OfX.Azure.ServiceBus.Implementations;

internal class AzureServiceBusClient(IServiceProvider serviceProvider) : IRequestClient
{
    public async Task<ItemsResponse<OfXDataResponse>> RequestAsync<TAttribute>(
        RequestContext<TAttribute> requestContext) where TAttribute : OfXAttribute
    {
        var client = serviceProvider.GetRequiredService<OpenAzureServiceBusClient<TAttribute>>();
        var result = await client.RequestAsync(requestContext);
        return result;
    }
}