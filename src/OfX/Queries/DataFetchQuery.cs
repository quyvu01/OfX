namespace OfX.Queries;

/// <summary>
/// Represents a query for fetching data from the OfX data provider.
/// </summary>
/// <param name="SelectorIds">
/// An array of string-based selector IDs identifying the entities to fetch.
/// </param>
/// <param name="Expressions">
/// A list of expression strings defining which properties to retrieve and how to project them.
/// </param>
public sealed record DataFetchQuery(string[] SelectorIds, string[] Expressions);