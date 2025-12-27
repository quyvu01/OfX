using OfX.Attributes;

namespace OfX.Tests.TestData.Attributes;

public sealed class ProvinceOfAttribute(string propertyName) : OfXAttribute(propertyName);
