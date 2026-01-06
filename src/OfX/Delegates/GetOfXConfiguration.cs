using OfX.Abstractions;

namespace OfX.Delegates;

/// <summary>
/// Delegate for retrieving the OfX configuration for a specific model and attribute type combination.
/// </summary>
/// <param name="modelType">The CLR type of the model entity.</param>
/// <param name="ofxAttributeType">The type of the <see cref="Attributes.OfXAttribute"/>.</param>
/// <returns>
/// The <see cref="IOfXConfigAttribute"/> containing the ID and default property configuration.
/// </returns>
public delegate IOfXConfigAttribute GetOfXConfiguration(Type modelType, Type ofxAttributeType);