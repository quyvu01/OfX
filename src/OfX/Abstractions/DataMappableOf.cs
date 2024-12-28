using OfX.Queries.OfXQueries;

namespace OfX.Abstractions;

public sealed record DataMappableOf<TAttribute>(List<string> SelectorIds, string Expression)
    : GetDataMappableQuery(SelectorIds, Expression), IDataMappableOf<TAttribute>
    where TAttribute : Attribute, IDataMappableCore;