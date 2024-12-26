using Microsoft.Extensions.DependencyInjection;
using OfX.Abstractions;
using OfX.Grpc.Delegates;
using OfX.Implementations;
using OfX.Queries.CrossCuttingQueries;
using OfX.Responses;

namespace OfX.Grpc.Abstractions;

public interface IOfXGrpcRequestClient<in TQuery, TAttribute> : IMappableRequestHandler<TQuery, TAttribute>
    where TQuery : DataMappableOf<TAttribute> where TAttribute : OfXAttribute
{
    IServiceProvider ServiceProvider { get; }

    async Task<ItemsResponse<OfXDataResponse>> IMappableRequestHandler<TQuery, TAttribute>.RequestAsync(
        RequestContext<TQuery> context)
    {
        var ofXResponseFunc = ServiceProvider.GetRequiredService<GetOfXResponseFunc>();
        var func = ofXResponseFunc.Invoke(typeof(TQuery));
        return await func.Invoke(new GetDataMappableQuery(context.Query.SelectorIds, context.Query.Expression),
            new Context(context.Headers, context.CancellationToken));
    }
}