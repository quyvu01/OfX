using OfX.Abstractions;
using OfX.Tests.Attributes;

namespace OfX.Tests.Contracts;

public sealed record GetCrossCuttingUsersQuery(List<string> SelectorIds, string Expression)
    : DataMappableOf<UserOfAttribute>(SelectorIds, Expression);