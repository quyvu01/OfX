using OfX.Responses;

namespace OfX.Abstractions;

public interface IMappableRequestHandler<in TRequest, TAttribute> where TRequest : DataMappableOf<TAttribute>
    where TAttribute : OfXAttribute

{
    Task<ItemsResponse<OfXDataResponse>> RequestAsync(RequestContext<TRequest> request);
}