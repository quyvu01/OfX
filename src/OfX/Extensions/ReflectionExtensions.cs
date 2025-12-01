using OfX.Helpers;

namespace OfX.Extensions;

internal static class ReflectionExtensions
{
    internal static bool IsConcrete(this Type type) => type is { IsClass: true, IsAbstract: false, IsInterface: false };
    internal static bool IsPrimitiveType(this Type type) => GeneralHelpers.IsPrimitiveType(type);
}