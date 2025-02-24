using OfX.Attributes;

namespace Kernel.Attributes;

public sealed class MemberAddressOfAttribute(string propertyName) : OfXAttribute(propertyName);