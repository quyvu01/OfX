using OfX.Attributes;
using OfX.Responses;

namespace OfX.Abstractions;

/// <summary>
/// Represents the orchestrator responsible for executing all received pipelines 
/// for a specific <see cref="OfXAttribute"/> type.
/// </summary>
/// <typeparam name="TAttribute">
/// The <see cref="OfXAttribute"/> type that defines the request's context and processing rules.
/// </typeparam>
/// <remarks>
/// <para>
/// The <see cref="IReceivedPipelinesOrchestrator{TAttribute}"/> is the **entry point** of the server-side 
/// processing pipeline for a given <typeparamref name="TAttribute"/>.
/// </para>
/// <para>
/// Its responsibilities include:
/// </para>
/// <list type="number">
/// <item>Creating and preparing the <see cref="RequestContext{TAttribute}"/>.</item>
/// <item>Executing all registered <see cref="IReceivedPipelineBehavior{TAttribute}"/> instances in order.</item>
/// <item>Delegating to the final <see cref="IQueryOfHandler{TModel, TAttribute}"/> to retrieve data.</item>
/// </list>
/// This interface provides a type-safe orchestration layer that ensures all middleware and handlers 
/// are executed in the correct order before producing a response.
/// </remarks>
public interface IReceivedPipelinesOrchestrator<TAttribute> : IOfXBase<TAttribute>
    where TAttribute : OfXAttribute
{
    /// <summary>
    /// Executes the entire received pipeline for the specified request context.
    /// </summary>
    /// <param name="requestContext">
    /// The strongly-typed request context, which contains selector IDs, expressions, headers, 
    /// and the <see cref="CancellationToken"/>.
    /// </param>
    /// <returns>
    /// A task that produces an <see cref="ItemsResponse{OfXDataResponse}"/> representing the 
    /// final result after all pipeline behaviors and the query handler have executed.
    /// </returns>
    Task<ItemsResponse<OfXDataResponse>> ExecuteAsync(RequestContext<TAttribute> requestContext);
}