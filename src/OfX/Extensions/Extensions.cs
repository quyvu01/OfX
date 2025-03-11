using OfX.Attributes;
using OfX.Exceptions;

namespace OfX.Extensions;

public static class Extensions
{
    public static void ForEach<T>(this IEnumerable<T> src, Action<T> action)
    {
        foreach (var item in src ?? []) action?.Invoke(item);
    }

    public static void IteratorVoid<T>(this IEnumerable<T> src) => src.ForEach(_ => { });

    internal static void MustBeOfXAttribute(this Type type)
    {
        if (type is null) throw new ArgumentNullException(nameof(type));
        if (!typeof(OfXAttribute).IsAssignableFrom(type)) throw new OfXException.TypeIsNotOfXAttribute(type);
    }
}