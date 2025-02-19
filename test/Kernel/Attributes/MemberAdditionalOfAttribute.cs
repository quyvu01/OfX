using OfX.Attributes;

namespace Kernel.Attributes;

public class MemberAdditionalOfAttribute(string propertyName) : OfXAttribute(propertyName);