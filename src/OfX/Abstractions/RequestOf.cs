using OfX.Attributes;

namespace OfX.Abstractions;

/// <summary>
/// We will create the request based on RequestOf!
/// </summary>
/// <param name="SelectorIds"></param>
/// <param name="Expression"></param>
/// <typeparam name="TAttribute"></typeparam>
public sealed record RequestOf<TAttribute>(List<string> SelectorIds, string Expression) where TAttribute : OfXAttribute;