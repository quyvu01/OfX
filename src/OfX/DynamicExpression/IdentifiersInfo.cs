namespace OfX.DynamicExpression;

public class IdentifiersInfo(
    IEnumerable<string> unknownIdentifiers,
    IEnumerable<Identifier> identifiers,
    IEnumerable<ReferenceType> types)
{
    public IEnumerable<string> UnknownIdentifiers { get; private set; } = unknownIdentifiers.ToList();
    public IEnumerable<Identifier> Identifiers { get; private set; } = identifiers.ToList();
    public IEnumerable<ReferenceType> Types { get; private set; } = types.ToList();
}