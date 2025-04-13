using OfX.Attributes;

namespace Shared.Attributes;

public sealed class UserOfAttribute(string propertyName) : OfXAttribute(propertyName);