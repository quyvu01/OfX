using OfX.Abstractions;

namespace OfX.ApplicationModels;

public sealed record OfXModelData(Type ModelType, Type OfXAttributeType, IOfXConfigAttribute OfXConfigAttribute);