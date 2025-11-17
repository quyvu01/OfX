using OfX.Attributes;

namespace OfX.BenchmarkLab.Attributes;

public sealed class UserOfAttribute(string propertyName) : OfXAttribute(propertyName); 