using OfX.Responses;

namespace OfX.Queries.CrossCuttingQueries;

public record GetDataMappableQuery(List<string> SelectorIds, string Expression)
    : IQueryCollection<CrossCuttingDataResponse>;