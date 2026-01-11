using System.Collections;
using OfX.Cached;
using OfX.Extensions;

namespace OfX.Benchmark.OfXBenchmarks.Reflections;

internal static class ReflectionWithFastCache
{
    internal static IEnumerable<FastCacheMappableDataProperty> GetMappableProperties(object rootObject)
    {
        if (InvalidObject(rootObject)) yield break;
        Stack<object> stack = [];
        switch (rootObject)
        {
            case IEnumerable enumerable:
                EnumerableObject(enumerable, stack);
                break;
            default:
                stack.Push(rootObject);
                break;
        }

        while (stack.TryPop(out var obj))
            foreach (var mappableDataProperty in GetMappableProperties(obj, stack))
                yield return mappableDataProperty;
    }

    private static IEnumerable<FastCacheMappableDataProperty> GetMappableProperties(object obj, Stack<object> stack)
    {
        if (InvalidObject(obj)) yield break;
        if (obj is IEnumerable enumerable) EnumerableObject(enumerable, stack);
        var objType = obj.GetType();
        var objectCached = OfXModelCache.GetModel(objType);
        foreach (var (propertyInfo, accessor) in objectCached.Accessors)
        {
            var propertyInformation = objectCached.GetInformation(propertyInfo);
            yield return new FastCacheMappableDataProperty(propertyInfo, obj, propertyInformation);
            if (propertyInformation.RequiredAccessor is not null) continue;
            // Currently, I just agree this one, need to update to ignore required property i.e: UserId
            var propValue = accessor.Get(obj);
            if (InvalidObject(propValue)) continue;
            foreach (var value in GetMappableProperties(propValue, stack)) yield return value;
        }
    }

    private static bool InvalidObject(object obj) => obj.IsNullOrPrimitive();

    private static void EnumerableObject(IEnumerable propertyValue, Stack<object> stack)
    {
        foreach (var item in propertyValue)
        {
            if (InvalidObject(item)) continue;
            if (item is IEnumerable enumerable)
            {
                EnumerableObject(enumerable, stack);
                continue;
            }

            if (!stack.Contains(item)) stack.Push(item);
        }
    }
}