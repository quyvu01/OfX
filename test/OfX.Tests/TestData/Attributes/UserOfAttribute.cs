using OfX.Attributes;

namespace OfX.Tests.TestData.Attributes;

public sealed class UserOfAttribute(string propertyName) : OfXAttribute(propertyName);
