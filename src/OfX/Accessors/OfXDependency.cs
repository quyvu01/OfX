namespace OfX.Accessors;

public sealed record OfXDependency(
    int Order,
    string Expression,
    Type RuntimeAttributeType,
    IOfXPropertyAccessor RequiredAccessor);