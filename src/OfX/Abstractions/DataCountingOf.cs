using OfX.Queries.CrossCuttingQueries;

namespace OfX.Abstractions;

public record DataCountingOf<TAttribute>(List<string> Selectors, string Expression)
    : GetDataCountingQuery(Selectors, Expression), IDataCountingOf<TAttribute>
    where TAttribute : Attribute, IDataCountingCore;