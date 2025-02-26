using OfX.Attributes;

namespace Kernel.Attributes;

public sealed class MemberSocialOfAttribute(string propertyName) : OfXAttribute(propertyName);