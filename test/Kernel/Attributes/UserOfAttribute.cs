using OfX.Attributes;

namespace Kernel.Attributes;

public sealed class UserOfAttribute(string propertyName) : OfXAttribute(propertyName);