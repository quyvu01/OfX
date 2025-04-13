using OfX.Attributes;

namespace Shared.Attributes;

public class MemberAdditionalOfAttribute(string propertyName) : OfXAttribute(propertyName);