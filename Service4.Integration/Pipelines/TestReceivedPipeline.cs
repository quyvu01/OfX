using OfX.Abstractions;
using Serilog;
using Shared.Attributes;

namespace Service4.Integration.Pipelines;

public class TestReceivedPipeline : 
    ICustomExpressionBehavior<ExternalDataOfAttribute>
{
    public string CustomExpression() => "Test";

    public async Task<Dictionary<string, object>> HandleAsync(RequestContext<ExternalDataOfAttribute> requestContext)
    {
        await Task.Delay(TimeSpan.FromSeconds(0.7));
        Log.Information("Received data for Test: {@TestContext}", requestContext.Query);
        return requestContext.Query.SelectorIds.ToDictionary(kv => kv, object (kv) => $"ExternalDataOf-Test: {kv}");
    }
}