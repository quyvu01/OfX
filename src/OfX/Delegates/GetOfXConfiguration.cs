using OfX.Abstractions;

namespace OfX.Delegates;

public delegate IOfXConfigAttribute GetOfXConfiguration(Type modelType, Type ofxAttributeType);