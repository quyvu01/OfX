using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using OfX.Abstractions;
using OfX.ApplicationModels;
using OfX.Attributes;
using OfX.Constants;
using OfX.Extensions;
using OfX.Helpers;
using OfX.Responses;

namespace OfX.Implementations;

internal abstract class SendPipelinesOrchestrator
{
    internal abstract Task<ItemsResponse<OfXDataResponse>> ExecuteAsync(OfXRequest message, IContext context);
}

internal sealed class SendPipelinesOrchestrator<TAttribute>(IServiceProvider serviceProvider) :
    SendPipelinesOrchestrator where TAttribute : OfXAttribute
{
    internal override async Task<ItemsResponse<OfXDataResponse>> ExecuteAsync(OfXRequest message, IContext context)
    {
        var handler = serviceProvider.GetRequiredService<IMappableRequestHandler<TAttribute>>();
        var cancellationToken = context?.CancellationToken ?? CancellationToken.None;
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(OfXConstants.DefaultRequestTimeout);
        var expressions = JsonSerializer.Deserialize<string[]>(message.Expression);
        var parameters = context is IExpressionParameters expressionParameters ? expressionParameters.Parameters : null;

        // Resolve expressions and build lookup in one pass

        var (expressionMap, resolvedExpressions) = expressions.Aggregate((
                ExpressionMap: new Dictionary<ExpressionWrapper, string>(expressions.Length),
                ResolvedExpressions: new List<string>(expressions.Length)),
            (acc, originalExpression) =>
            {
                var resolvedExpression = RegexHelpers.ResolvePlaceholders(originalExpression, parameters);
                if (acc.ExpressionMap.TryAdd(new ExpressionWrapper(resolvedExpression), originalExpression))
                    acc.ResolvedExpressions.Add(resolvedExpression);
                return acc;
            });

        // Serialize only the distinct resolved expressions
        var expression = JsonSerializer.Serialize(resolvedExpressions);

        var request = new RequestOf<TAttribute>(message.SelectorIds, expression);
        var requestContext = new RequestContextImpl<TAttribute>(request, context?.Headers ?? [], cts.Token);
        var result = await serviceProvider
            .GetServices<ISendPipelineBehavior<TAttribute>>()
            .Reverse()
            .Aggregate(() => handler.RequestAsync(requestContext),
                (acc, pipeline) => () => pipeline.HandleAsync(requestContext, acc)).Invoke();

        result.Items.ForEach(it =>
        {
            var valueLookup = it.OfXValues
                .ToDictionary(v => new ExpressionWrapper(v.Expression), v => v);

            var values = ValueResponses(valueLookup);
            it.OfXValues = [..values];
        });
        return result;

        IEnumerable<OfXValueResponse> ValueResponses(Dictionary<ExpressionWrapper, OfXValueResponse> valueLookup)
        {
            foreach (var value in expressionMap)
                if (valueLookup.TryGetValue(value.Key, out var valueResult))
                    yield return new OfXValueResponse { Expression = value.Value, Value = valueResult.Value };
        }
    }
}

internal readonly record struct ExpressionWrapper(string Expression)
{
    public bool Equals(ExpressionWrapper other) =>
        string.Equals(Expression, other.Expression, StringComparison.Ordinal);

    public override int GetHashCode() => Expression?.GetHashCode() ?? 0;
}