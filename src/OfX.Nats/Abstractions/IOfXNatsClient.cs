using Microsoft.Extensions.DependencyInjection;
using OfX.Abstractions;
using OfX.Attributes;
using OfX.Responses;

namespace OfX.Nats.Abstractions;

public interface IOfXNatsClient<TAttribute> : IMappableRequestHandler<TAttribute> where TAttribute : OfXAttribute
{
    IServiceProvider ServiceProvider { get; }

    async Task<ItemsResponse<OfXDataResponse>> IMappableRequestHandler<TAttribute>.RequestAsync(
        RequestContext<TAttribute> context)
    {
        var natRequesterService = ServiceProvider.GetRequiredService<INatsRequester<TAttribute>>();
        var result = await natRequesterService.RequestAsync(context);
        return result;
    }
}