using OfX.Attributes;

namespace OfX.Abstractions;

/// <summary>
/// Represents a request context, which is created for each request.
/// When invoking <see cref="IDistributedMapper.MapDataAsync"/>, you may pass an instance of <c>IContext</c>.
/// This parameter is optional.
/// </summary>
/// <remarks>
/// <para><b>Headers:</b> A dictionary for sending additional metadata to the server.  
/// You can include anything here, and handle it in the request as needed.</para>
/// <para><b>CancellationToken:</b> Used to cancel a request (for example, after a timeout).</para>
/// </remarks>
public interface IContext
{
    /// <summary>
    /// A collection of key-value pairs containing additional metadata sent with the request.
    /// </summary>
    Dictionary<string, string> Headers { get; }

    /// <summary>
    /// A token used to cancel the request if necessary (e.g., due to timeout or user action).
    /// </summary>
    CancellationToken CancellationToken { get; }
}

/// <summary>
/// Represents the context for a received request, including headers and cancellation support.
/// </summary>
/// <typeparam name="TAttribute">
/// The type of <see cref="OfXAttribute"/> that describes the request.
/// </typeparam>
public interface RequestContext<TAttribute> : IOfXBase<TAttribute>, IContext where TAttribute : OfXAttribute
{
    /// <summary>
    /// The request payload and metadata for the given <typeparamref name="TAttribute"/>.
    /// </summary>
    OfXQueryRequest<TAttribute> Query { get; }
}
