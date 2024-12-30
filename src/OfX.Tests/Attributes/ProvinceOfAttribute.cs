using OfX.Attributes;

namespace OfX.Tests.Attributes;

public sealed class ProvinceOfAttribute(string propertyName) : OfXAttribute(propertyName);