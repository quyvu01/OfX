using OfX.Attributes;

namespace Shared.Attributes;

public sealed class MemberAddressOfAttribute(string propertyName) : OfXAttribute(propertyName);