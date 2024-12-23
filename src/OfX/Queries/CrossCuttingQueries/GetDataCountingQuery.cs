using OfX.Responses;

namespace OfX.Queries.CrossCuttingQueries;

public record GetDataCountingQuery(List<string> Selectors, string Expression)
    : IQueryCollection<CrossCuttingDataResponse>;