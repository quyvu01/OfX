using OfX.Abstractions;
using Shared.Attributes;

namespace Service2.Pipelines;

public sealed class TestCustomUseData : ICustomExpressionBehavior<UserOfAttribute>
{
    public string CustomExpression() => "CustomExpression";

    public async Task<Dictionary<string, object>> HandleAsync(RequestContext<UserOfAttribute> requestContext)
    {
        // await Task.Delay(TimeSpan.FromSeconds(0.5));
        await Task.Yield();
        return requestContext.Query.SelectorIds.ToDictionary(kv => kv, object (kv) => $"Hello: {kv}");
    }
}