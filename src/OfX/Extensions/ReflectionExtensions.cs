using OfX.Helpers;

namespace OfX.Extensions;

internal static class ReflectionExtensions
{
    extension(Type type)
    {
        internal bool IsConcrete() => type is { IsClass: true, IsAbstract: false, IsInterface: false };
        internal bool IsPrimitiveType() => GeneralHelpers.IsPrimitiveType(type);
    }
}