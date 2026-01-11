namespace OfX.HotChocolate.Attributes;

/// <summary>
/// Marks a GraphQL resolver parameter as containing runtime expression parameters.
/// </summary>
/// <remarks>
/// Parameters marked with this attribute will be extracted and made available
/// for OfX expression placeholder resolution (e.g., "${paramName|default}").
/// </remarks>
/// <example>
/// <code>
/// public async Task&lt;User&gt; GetUser(string id, [Parameters] UserQueryParams parameters)
/// {
///     // parameters.Language will resolve ${language|en} in expressions
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class ParametersAttribute : Attribute;