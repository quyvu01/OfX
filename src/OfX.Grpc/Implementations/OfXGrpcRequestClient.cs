using OfX.Abstractions;
using OfX.ApplicationModels;
using OfX.Attributes;
using OfX.Grpc.Delegates;
using OfX.Implementations;
using OfX.Responses;

namespace OfX.Grpc.Implementations;

internal class OfXGrpcRequestClient<TAttribute>(GetOfXResponseFunc ofXResponseFunc)
    : IMappableRequestHandler<TAttribute>
    where TAttribute : OfXAttribute
{
    public async Task<ItemsResponse<OfXDataResponse>> RequestAsync(RequestContext<TAttribute> requestContext)
    {
        var func = ofXResponseFunc.Invoke(typeof(TAttribute));
        return await func.Invoke(
            new MessageDeserializable(requestContext.Query.SelectorIds, requestContext.Query.Expression),
            new Context(requestContext.Headers, requestContext.CancellationToken));
    }
}