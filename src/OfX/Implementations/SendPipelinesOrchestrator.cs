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

/// <summary>
/// Abstract base class for client-side pipeline orchestration in the OfX framework.
/// </summary>
/// <remarks>
/// This class provides the abstract contract for executing send pipelines on the client side
/// before requests are transmitted to remote services.
/// </remarks>
internal abstract class SendPipelinesOrchestrator
{
    /// <summary>
    /// Executes the send pipeline and returns the data response.
    /// </summary>
    /// <param name="message">The OfX request containing selector IDs and expressions.</param>
    /// <param name="context">Optional context containing headers and parameters.</param>
    /// <returns>The items response containing the fetched data.</returns>
    internal abstract Task<ItemsResponse<DataResponse>> ExecuteAsync(OfXRequest message, IContext context);
}

/// <summary>
/// Client-side pipeline orchestrator that executes send pipeline behaviors before transport.
/// </summary>
/// <typeparam name="TAttribute">The OfX attribute type for which pipelines are being executed.</typeparam>
/// <param name="serviceProvider">The service provider for resolving handlers and pipeline behaviors.</param>
/// <remarks>
/// This orchestrator:
/// <list type="bullet">
///   <item><description>Resolves expression placeholders with runtime parameters</description></item>
///   <item><description>Executes send pipeline behaviors in reverse order (middleware pattern)</description></item>
///   <item><description>Handles timeout using the configured default request timeout</description></item>
///   <item><description>Maps resolved expressions back to original expressions in the response</description></item>
/// </list>
/// </remarks>
internal sealed class SendPipelinesOrchestrator<TAttribute>(IServiceProvider serviceProvider) :
    SendPipelinesOrchestrator where TAttribute : OfXAttribute
{
    internal override async Task<ItemsResponse<DataResponse>> ExecuteAsync(OfXRequest message, IContext context)
    {
        var handler = serviceProvider.GetRequiredService<IClientRequestHandler<TAttribute>>();
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

        IEnumerable<ValueResponse> ValueResponses(Dictionary<ExpressionWrapper, ValueResponse> valueLookup)
        {
            foreach (var value in expressionMap)
                if (valueLookup.TryGetValue(value.Key, out var valueResult))
                    yield return new ValueResponse { Expression = value.Value, Value = valueResult.Value };
        }
    }
}

/// <summary>
/// A wrapper struct for expressions that provides case-sensitive ordinal comparison.
/// </summary>
/// <param name="Expression">The expression string to wrap.</param>
/// <remarks>
/// This struct is used to ensure consistent expression matching using ordinal string comparison,
/// which is important for expression lookup when mapping results back to original requests.
/// </remarks>
internal readonly record struct ExpressionWrapper(string Expression)
{
    /// <summary>
    /// Determines equality using case-sensitive ordinal string comparison.
    /// </summary>
    public bool Equals(ExpressionWrapper other) =>
        string.Equals(Expression, other.Expression, StringComparison.Ordinal);

    /// <inheritdoc />
    public override int GetHashCode() => Expression?.GetHashCode() ?? 0;
}