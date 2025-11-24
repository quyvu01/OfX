namespace OfX.Extensions;

internal static class ReflectionExtensions
{
    internal static bool IsConcrete(this Type type) => type is { IsClass: true, IsAbstract: false, IsInterface: false };
}