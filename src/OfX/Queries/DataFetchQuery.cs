namespace OfX.Queries;

public sealed record DataFetchQuery(string[] SelectorIds, List<string> Expressions);