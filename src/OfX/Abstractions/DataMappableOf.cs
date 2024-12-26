using OfX.Queries.CrossCuttingQueries;

namespace OfX.Abstractions;

public abstract record DataMappableOf<TAttribute>(List<string> SelectorIds, string Expression)
    : GetDataMappableQuery(SelectorIds, Expression), IDataMappableOf<TAttribute>
    where TAttribute : Attribute, IDataMappableCore;