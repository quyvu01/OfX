using Microsoft.Extensions.DependencyInjection;
using OfX.Abstractions;
using OfX.ApplicationModels;
using OfX.Attributes;
using OfX.Grpc.Delegates;
using OfX.Implementations;
using OfX.Responses;

namespace OfX.Grpc.Abstractions;

public interface IOfXGrpcRequestClient<TAttribute> : IMappableRequestHandler<TAttribute>
    where TAttribute : OfXAttribute
{
    IServiceProvider ServiceProvider { get; }

    async Task<ItemsResponse<OfXDataResponse>> IMappableRequestHandler<TAttribute>.RequestAsync(
        RequestContext<TAttribute> requestContext)
    {
        var ofXResponseFunc = ServiceProvider.GetRequiredService<GetOfXResponseFunc>();
        var func = ofXResponseFunc.Invoke(typeof(TAttribute));
        return await func.Invoke(new MessageDeserializable
                { SelectorIds = requestContext.Query.SelectorIds, Expression = requestContext.Query.Expression },
            new Context(requestContext.Headers, requestContext.CancellationToken));
    }
}