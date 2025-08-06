using OfX.Attributes;

namespace Shared.Attributes;

public sealed class ExternalDataOfAttribute(string propertyName) : OfXAttribute(propertyName);