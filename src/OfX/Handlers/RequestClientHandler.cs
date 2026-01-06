using Microsoft.Extensions.DependencyInjection;
using OfX.Abstractions;
using OfX.Abstractions.Transporting;
using OfX.Attributes;
using OfX.Exceptions;
using OfX.Responses;

namespace OfX.Handlers;

/// <summary>
/// Internal handler that routes client requests through the configured <see cref="IRequestClient"/> transport.
/// </summary>
/// <typeparam name="TAttribute">The OfX attribute type for this handler.</typeparam>
/// <param name="serviceProvider">The service provider for resolving the transport client.</param>
internal sealed class RequestClientHandler<TAttribute>(IServiceProvider serviceProvider)
    : IClientRequestHandler<TAttribute> where TAttribute : OfXAttribute
{
    /// <inheritdoc />
    public async Task<ItemsResponse<OfXDataResponse>> RequestAsync(RequestContext<TAttribute> requestContext)
    {
        var client = serviceProvider.GetService<IRequestClient>();
        if (client is null) throw new OfXException.NoHandlerForAttribute(typeof(TAttribute));
        var result = await client.RequestAsync(requestContext);
        return result;
    }
}