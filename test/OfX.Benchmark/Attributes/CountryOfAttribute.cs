using OfX.Attributes;

namespace OfX.Benchmark.Attributes;

public class CountryOfAttribute(string propertyName) : OfXAttribute(propertyName);