using OfX.Attributes;

namespace OfX.Tests.TestData.Attributes;

public sealed class CountryOfAttribute(string propertyName) : OfXAttribute(propertyName);
