using OfX.Attributes;

namespace OfX.Abstractions;

/// <summary>
/// Provides a way to define and handle custom expressions that do not directly map to a model expression.
/// </summary>
/// <remarks>
/// This interface is intended for server-side implementations only.  
/// For example, if a client requests a <c>SpecialExpression</c> that does not match any existing model expression,  
/// you can implement this interface to define the custom expression and provide a handler for it.
/// </remarks>
/// <typeparam name="TAttribute">
/// The type of <see cref="OfXAttribute"/> associated with the custom expression.
/// </typeparam>
public interface ICustomExpressionBehavior<TAttribute> where TAttribute : OfXAttribute
{
    /// <summary>
    /// Defines the custom expression name or key that the server should recognize.
    /// </summary>
    /// <returns>The string representation of the custom expression.</returns>
    string CustomExpression();

    /// <summary>
    /// Handles the custom expression request asynchronously.
    /// </summary>
    /// <param name="requestContext">The context containing request data, headers, and cancellation support.</param>
    /// <returns>
    /// A dictionary of key-value pairs representing the result of the custom expression.
    /// </returns>
    Task<Dictionary<string, object>> HandleAsync(RequestContext<TAttribute> requestContext);
}
