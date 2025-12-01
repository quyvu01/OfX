namespace OfX.Accessors;

public sealed record PropertyInformation(
    int Order,
    string Expression,
    Type RuntimeAttributeType,
    IOfXPropertyAccessor RequiredAccessor);