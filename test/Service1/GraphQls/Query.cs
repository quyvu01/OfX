using Newtonsoft.Json;
using OfX.Abstractions;
using OfX.Attributes;
using OfX.Extensions;
using OfX.Queries;
using Service1.Contract.Responses;

namespace Service1.GraphQls;

public class Query
{
    public List<MemberResponse> GetMembers()
    {
        return
        [
            .. Enumerable.Range(1, 1).Select(a => new MemberResponse
            {
                Id = a.ToString(),
                UserId = a.ToString(), MemberAdditionalId = a.ToString(),
                MemberAddressId = a.ToString(),
                MemberSocialId = a.ToString()
            })
        ];
    }
}

public class ResponseType<TResponse> : ObjectType<TResponse>
{
    protected override void Configure(IObjectTypeDescriptor<TResponse> descriptor)
    {
        typeof(TResponse)
            .GetProperties()
            .Where(p => p.GetCustomAttributes(true)
                .Any(a => typeof(OfXAttribute).IsAssignableFrom(a.GetType())))
            .ForEach(prop => descriptor.Field(prop)
                .Use(next => async context =>
                {
                    var currentContext = context.Service<ICurrentContextProvider>();
                    var ctx = currentContext.CreateContext();
                    ctx.TargetPropertyInfo = prop;
                    // Temp for test
                    var attribute = prop.GetCustomAttributes(true)
                        .OfType<OfXAttribute>().First();
                    ctx.Expression = attribute.Expression;
                    ctx.RuntimeAttributeType = attribute.GetType();
                    ctx.SelectorPropertyName = attribute.PropertyName;
                    await next(context);
                })
                .ResolveWith<MemberResolvers<TResponse>>(x =>
                    x.GetDataAsync(default!, null, null!, CancellationToken.None!)));
    }
}

public sealed class MemberResolvers<TResponse>
{
    public async Task<string> GetDataAsync(
        [Parent] TResponse response, ICurrentContextProvider currentContextProvider,
        DataMappingLoader dataMappingLoader, CancellationToken ct)
    {
        var currentContext = currentContextProvider.GetContext();
        var selectorId = typeof(TResponse)
            .GetProperty(currentContext.SelectorPropertyName)?
            .GetValue(response)?.ToString();
        var data = await dataMappingLoader
            .LoadAsync(new ExpressionData(selectorId, currentContext.Expression,
                currentContext.RuntimeAttributeType), ct);
        if (data is null) return null;
        var result = JsonConvert.DeserializeObject(data, currentContext.TargetPropertyInfo.PropertyType);
        return result as string;
    }
}

public class DataMappingLoader(
    IBatchScheduler batchScheduler,
    DataLoaderOptions options,
    IDataMappableService dataMappableService)
    : BatchDataLoader<ExpressionData, string>(batchScheduler, options)
{
    protected override async Task<IReadOnlyDictionary<ExpressionData, string>> LoadBatchAsync(
        IReadOnlyList<ExpressionData> keys, CancellationToken cancellationToken)
    {
        var tasks = keys.GroupBy(a => a.AttributeType)
            .Select(async gr =>
            {
                var ids = gr.Select(k => k.SelectorId).Distinct();
                var expressions = gr.Select(k => k.Expression);
                var result = await dataMappableService
                    .FetchDataAsync(gr.Key, new DataFetchQuery([..ids], [..expressions]));
                var res = result.Items.Join(gr, a => a.Id, k => k.SelectorId, (a, k) => (a, k))
                    .ToDictionary(x => x.k,
                        x => { return x.a.OfXValues.FirstOrDefault(a => a.Expression == x.k.Expression)?.Value; });
                return res;
            });
        var result = await Task.WhenAll(tasks);
        return keys.ToDictionary(a => a,
            ex => result.Select(k => k.GetValueOrDefault(ex)).FirstOrDefault(x => x != null));
    }
}

public sealed record ExpressionData(string SelectorId, string Expression, Type AttributeType);