using OfX.Abstractions;
using OfX.ApplicationModels;
using OfX.Attributes;
using OfX.Grpc.Delegates;
using OfX.Grpc.Internals;
using OfX.Responses;

namespace OfX.Grpc.Implementations;

internal class GrpcClient<TAttribute>(GetOfXResponseFunc ofXResponseFunc)
    : IMappableRequestHandler<TAttribute>
    where TAttribute : OfXAttribute
{
    public async Task<ItemsResponse<OfXDataResponse>> RequestAsync(RequestContext<TAttribute> requestContext)
    {
        var func = ofXResponseFunc.Invoke(typeof(TAttribute));
        return await func.Invoke(
            new OfXRequest(requestContext.Query.SelectorIds, requestContext.Query.Expression),
            new GrpcClientContext(requestContext.Headers, requestContext.CancellationToken));
    }
}