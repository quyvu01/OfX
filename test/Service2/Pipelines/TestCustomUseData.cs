using OfX.Abstractions;
using OfX.Responses;
using Shared.Attributes;

namespace Service2.Pipelines;

public sealed class TestCustomUseData : ICustomExpressionBehavior<UserOfAttribute>
{
    public string CustomExpression() => "CustomExpression";

    public async Task<ItemsResponse<OfXCustomDataResponse>> HandleAsync(RequestContext<UserOfAttribute> requestContext)
    {
        await Task.Delay(TimeSpan.FromSeconds(0.5));
        return new ItemsResponse<OfXCustomDataResponse>([
            ..requestContext.Query.SelectorIds.Select(a => new OfXCustomDataResponse { Id = a, Value = $"Hello: {a}" })
        ]);
    }
}