using System.Collections;
using OfX.ApplicationModels;
using OfX.Cached;
using OfX.Extensions;
using OfX.Responses;
using OfX.Serializable;
using OfX.Statics;

namespace OfX.Helpers;

internal static class ReflectionHelpers
{
    internal static IEnumerable<MappableDataProperty> GetMappableProperties(object rootObject)
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

        while (stack.Count > 0)
        {
            var obj = stack.Pop();
            foreach (var mappableDataProperty in GetMappableProperties(obj, stack)) yield return mappableDataProperty;
        }
    }

    private static IEnumerable<MappableDataProperty> GetMappableProperties(object obj, Stack<object> stack)
    {
        if (InvalidObject(obj)) yield break;
        if (obj is IEnumerable enumerable) EnumerableObject(enumerable, stack);
        var objType = obj.GetType();
        var objectCached = OfXModelCache.GetModel(objType);
        foreach (var (propertyInfo, accessor) in objectCached.Accessors)
        {
            var dependency = objectCached.GetDependency(propertyInfo);
            yield return new MappableDataProperty(propertyInfo, obj, dependency);
            if (dependency.RequiredAccessor is not null) continue;
            // Currently, I just agree this one, need to update to ignore required property i.e: UserId
            var propValue = accessor.Get(obj);
            if (InvalidObject(propValue)) continue;
            foreach (var value in GetMappableProperties(propValue, stack)) yield return value;
        }
    }

    private static bool InvalidObject(object obj) => obj is null || GeneralHelpers.IsPrimitiveType(obj);

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

    // To use merge-expression, we have to group by attribute only, exclude expression as the older version!
    internal static IEnumerable<MappableTypeData> GetOfXTypesData(
        IEnumerable<MappableDataProperty> datas, IEnumerable<Type> ofXAttributeTypes) =>
        datas
            .GroupBy(x => new { AttributeType = x.Dependency.RuntimeAttributeType, x.Dependency.Order })
            .Join(ofXAttributeTypes, d => d.Key.AttributeType, x => x,
                (d, x) => new MappableTypeData(x, d
                        .Select(a => new RuntimePropertyCalling(a.Model, a.Dependency.RequiredAccessor)),
                    d.Select(a => a.Dependency.Expression), d.Key.Order));

    internal static void MapResponseData(IEnumerable<MappableDataProperty> mappableProperties,
        IEnumerable<(Type OfXAttributeType, ItemsResponse<OfXDataResponse> ItemsResponse)> dataFetched)
    {
        var dataWithExpression = dataFetched
            .Select(a => a.ItemsResponse.Items
                .Select(x => (x.Id, x.OfXValues))
                .Select(k => (a.OfXAttributeType, Data: k)))
            .SelectMany(x => x);
        mappableProperties.Join(dataWithExpression, ap => (ap.Dependency?.RuntimeAttributeType, ap.Dependency?
                .RequiredAccessor?
                .Get(ap.Model)?.ToString()),
            dt => (dt.OfXAttributeType, dt.Data.Id), (ap, dt) =>
            {
                var value = dt.Data
                    .OfXValues
                    .FirstOrDefault(a => a.Expression == ap.Dependency.Expression)?.Value;
                if (value is null || ap.PropertyInfo is not { } propertyInfo) return value;
                try
                {
                    var valueSet = SerializeObjects.DeserializeObject(value, propertyInfo.PropertyType);
                    var model = OfXModelCache.GetModel(ap.Model.GetType());
                    var accessor = model.GetAccessor(ap.PropertyInfo);
                    accessor?.Set(ap.Model, valueSet);
                }
                catch (Exception)
                {
                    if (OfXStatics.ThrowIfExceptions) throw;
                }

                return value;
            }).IteratorVoid();
    }
}