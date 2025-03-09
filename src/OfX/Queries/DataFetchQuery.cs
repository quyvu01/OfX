namespace OfX.Queries;

public sealed record DataFetchQuery(List<string> SelectorIds, List<string> Expressions);