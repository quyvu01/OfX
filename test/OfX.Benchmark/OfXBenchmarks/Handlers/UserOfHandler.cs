using System.Text.Json;
using OfX.Abstractions;
using OfX.Benchmark.Attributes;
using OfX.Responses;

namespace OfX.Benchmark.OfXBenchmarks.Handlers;

public sealed class UserOfHandler : IClientRequestHandler<UserOfAttribute>
{
    public Task<ItemsResponse<OfXDataResponse>> RequestAsync(RequestContext<UserOfAttribute> requestContext)
    {
        var query = requestContext.Query;
        var expressions = JsonSerializer.Deserialize<string[]>(query.Expression);
        var result = query.SelectorIds
            .Select(id => new OfXDataResponse
            {
                Id = id, OfXValues =
                [
                    ..expressions.Select(a =>
                    {
                        var valueResult = $"Some value from expression {a}";
                        return new OfXValueResponse
                        {
                            Expression = a, Value = JsonSerializer.Serialize(valueResult)
                        };
                    })
                ]
            });
        return Task.FromResult(new ItemsResponse<OfXDataResponse>([..result]));
    }
}