namespace OfX.ApplicationModels;

/// <summary>
/// Represents a serializable request payload for OfX data mapping operations.
/// </summary>
/// <remarks>
/// This record is used as the transport-level message format when sending requests
/// between clients and servers in distributed OfX scenarios.
/// </remarks>
/// <param name="SelectorIds">
/// An array of string-based selector IDs identifying the target entities to be queried.
/// These IDs will be converted to their appropriate types on the server side.
/// </param>
/// <param name="Expressions">
/// The expressions defining how to map or project the data.
/// Can be a simple property name or a complex expression with navigation and filtering.
/// </param>
public sealed record OfXRequest(string[] SelectorIds, string[] Expressions);