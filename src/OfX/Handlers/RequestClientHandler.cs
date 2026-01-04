using Microsoft.Extensions.DependencyInjection;
using OfX.Abstractions;
using OfX.Abstractions.Transporting;
using OfX.Attributes;
using OfX.Exceptions;
using OfX.Responses;

namespace OfX.Handlers;

internal sealed class RequestClientHandler<TAttribute>(IServiceProvider serviceProvider)
    : IMappableRequestHandler<TAttribute> where TAttribute : OfXAttribute
{
    public async Task<ItemsResponse<OfXDataResponse>> RequestAsync(RequestContext<TAttribute> requestContext)
    {
        var client = serviceProvider.GetService<IRequestClient>();
        if (client is null) throw new OfXException.NoHandlerForAttribute(typeof(TAttribute));
        var result = await client.RequestAsync(requestContext);
        return result;
    }
}