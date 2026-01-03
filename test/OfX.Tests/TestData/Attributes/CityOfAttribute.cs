using OfX.Attributes;

namespace OfX.Tests.TestData.Attributes;

public sealed class CityOfAttribute(string propertyName) : OfXAttribute(propertyName);
