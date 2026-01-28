using OfX.Attributes;

namespace OfX.Abstractions;

/// <summary>
/// Represents the raw request payload used to initiate a query in OfX.
/// </summary>
/// <typeparam name="TAttribute">
/// The <see cref="OfXAttribute"/> type that defines the mapping or behavior for the request.
/// </typeparam>
/// <param name="SelectorIds">
/// The list of string-based selector IDs identifying the target entities or records to be queried.  
/// These will later be converted into model IDs using <see cref="IIdConverter{TId}"/>.
/// </param>
/// <param name="Expressions">
/// The filter or selection expression (in string form) used to shape or restrict the query results.  
/// This expression will be parsed and executed by the server-side <see cref="IQueryOfHandler{TModel, TAttribute}"/>.
/// </param>
/// <remarks>
/// <para>
/// The <see cref="RequestOf{TAttribute}"/> is a lightweight, immutable record that holds the 
/// **essential request data** (selector IDs and expression).  
/// </para>
/// <para>
/// It is later wrapped in a <see cref="RequestContext{TAttribute}"/>, which adds additional context 
/// such as headers and <see cref="CancellationToken"/> for end-to-end request processing.
/// </para>
/// </remarks>
public sealed record RequestOf<TAttribute>(string[] SelectorIds, string[] Expressions) where TAttribute : OfXAttribute;