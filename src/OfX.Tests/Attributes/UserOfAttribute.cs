using OfX.Abstractions;

namespace OfX.Tests.Attributes;

public class UserOfAttribute(string propertyName) : OfXAttribute(propertyName);