using OfX.Attributes;

namespace Shared.Attributes;

public  sealed class MemberSocialOfAttribute(string propertyName) : OfXAttribute(propertyName);