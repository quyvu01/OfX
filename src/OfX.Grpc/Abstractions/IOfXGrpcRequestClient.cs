using Microsoft.Extensions.DependencyInjection;
using OfX.Abstractions;
using OfX.Attributes;
using OfX.Grpc.Delegates;
using OfX.Implementations;
using OfX.Queries.OfXQueries;
using OfX.Responses;

namespace OfX.Grpc.Abstractions;

public interface IOfXGrpcRequestClient<TAttribute> : IMappableRequestHandler<TAttribute>
    where TAttribute : OfXAttribute
{
    IServiceProvider ServiceProvider { get; }
    async Task<ItemsResponse<OfXDataResponse>> IMappableRequestHandler<TAttribute>.RequestAsync(RequestContext<TAttribute> context)
    {
        var ofXResponseFunc = ServiceProvider.GetRequiredService<GetOfXResponseFunc>();
        var func = ofXResponseFunc.Invoke(typeof(TAttribute));
        return await func.Invoke(new GetDataMappableQuery(context.Query.SelectorIds, context.Query.Expression),
            new Context(context.Headers, context.CancellationToken));
    }
}