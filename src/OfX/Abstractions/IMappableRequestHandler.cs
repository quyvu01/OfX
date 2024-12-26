using OfX.Responses;

namespace OfX.Abstractions;

public interface IMappableRequestHandler<in TRequest, TAttribute> : IMappableRequestHandler
    where TRequest : DataMappableOf<TAttribute>
    where TAttribute : OfXAttribute

{
    Task<ItemsResponse<OfXDataResponse>> RequestAsync(RequestContext<TRequest> context);
}

public interface IMappableRequestHandler;