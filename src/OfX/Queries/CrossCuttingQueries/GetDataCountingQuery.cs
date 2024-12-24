namespace OfX.Queries.CrossCuttingQueries;

public record GetDataCountingQuery(List<string> Selectors, string Expression);