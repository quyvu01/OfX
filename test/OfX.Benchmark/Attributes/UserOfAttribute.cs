using OfX.Attributes;

namespace OfX.Benchmark.Attributes;

public sealed class UserOfAttribute(string propertyName) : OfXAttribute(propertyName); 