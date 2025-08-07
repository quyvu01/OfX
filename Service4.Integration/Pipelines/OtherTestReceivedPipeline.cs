using OfX.Abstractions;
using Serilog;
using Shared.Attributes;

namespace Service4.Integration.Pipelines;

public sealed class OtherTestReceivedPipeline : ICustomExpressionBehavior<ExternalDataOfAttribute>
{
    public string CustomExpression() => "OtherTest";

    public async Task<Dictionary<string, object>> HandleAsync(RequestContext<ExternalDataOfAttribute> requestContext)
    {
        await Task.Delay(TimeSpan.FromSeconds(0.5));
        Log.Information("Received data for OtherTest: {@OtherTestContext}", requestContext.Query);
        return requestContext.Query.SelectorIds.ToDictionary(kv => kv, object (kv) => $"ExternalDataOf-OtherTest: {kv}");
    }
}