using OfX.Attributes;
using OfX.Responses;

namespace OfX.Abstractions;

public interface IMappableRequestHandler;

public interface IMappableRequestHandler<TAttribute> : IOfXBase<TAttribute>, IMappableRequestHandler
    where TAttribute : OfXAttribute

{
    Task<ItemsResponse<OfXDataResponse>> RequestAsync(RequestContext<TAttribute> requestContext);
}

internal class DefaultMappableRequestHandler<TAttribute> : IMappableRequestHandler<TAttribute>
    where TAttribute : OfXAttribute
{
    public Task<ItemsResponse<OfXDataResponse>> RequestAsync(RequestContext<TAttribute> requestContext) =>
        Task.FromResult(new ItemsResponse<OfXDataResponse>([]));
}