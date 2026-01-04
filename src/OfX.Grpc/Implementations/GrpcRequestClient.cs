using OfX.Abstractions;
using OfX.Abstractions.Transporting;
using OfX.ApplicationModels;
using OfX.Attributes;
using OfX.Grpc.Delegates;
using OfX.Grpc.Internals;
using OfX.Responses;

namespace OfX.Grpc.Implementations;

public sealed class GrpcRequestClient(GetOfXResponseFunc ofXResponseFunc) : IRequestClient
{
    public async Task<ItemsResponse<OfXDataResponse>> RequestAsync<TAttribute>(
        RequestContext<TAttribute> requestContext) where TAttribute : OfXAttribute
    {
        var func = ofXResponseFunc.Invoke(typeof(TAttribute));
        return await func.Invoke(
            new OfXRequest(requestContext.Query.SelectorIds, requestContext.Query.Expression),
            new GrpcClientContext(requestContext.Headers, requestContext.CancellationToken));
    }
}