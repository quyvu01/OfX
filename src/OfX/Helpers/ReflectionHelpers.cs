using System.Collections;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using OfX.ApplicationModels;
using OfX.Attributes;
using OfX.Extensions;
using OfX.ObjectContexts;
using OfX.Responses;
using OfX.Serializable;
using OfX.Statics;

namespace OfX.Helpers;

internal static class ReflectionHelpers
{
    private static readonly ConcurrentDictionary<PropertyInfo, MappableDataPropertyCache>
        OfXPropertiesCache = new();

    private static readonly ConcurrentDictionary<Type, Dictionary<PropertyInfo, PropertyContext[]>> Graphs = [];

    internal static IEnumerable<MappableDataProperty> GetMappableProperties(object rootObject)
    {
        if (rootObject is null || GeneralHelpers.IsPrimitiveType(rootObject)) yield break;
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
            if (obj is null || GeneralHelpers.IsPrimitiveType(obj)) continue;
            var objType = obj.GetType();
            var isCached = Graphs.TryGetValue(objType, out var graphs);
            if (isCached)
                foreach (var property in IterateMappableProperties(graphs.Keys, obj))
                    yield return property;
            var nextScanningProperties = isCached switch
            {
                true => objType.GetProperties()
                    .Where(a => !GeneralHelpers.IsPrimitiveType(a.PropertyType))
                    .Except(graphs.Keys),
                _ => objType.GetProperties()
            };
            foreach (var property in IterateMappableProperties(nextScanningProperties, obj)) yield return property;
        }

        yield break;

        IEnumerable<MappableDataProperty> IterateMappableProperties(IEnumerable<PropertyInfo> properties, object obj)
        {
            if (obj is IEnumerable objectAsEnumerable)
            {
                EnumerableObject(objectAsEnumerable, stack);
                yield break;
            }

            foreach (var property in properties)
            {
                if (OfXPropertiesCache.TryGetValue(property, out var propertyCache))
                {
                    yield return new MappableDataProperty(property, obj, propertyCache.Attribute,
                        propertyCache.Func, propertyCache.Expression, propertyCache.Order);
                    continue;
                }

                var ofXAttribute = property.GetCustomAttributes(true).OfType<OfXAttribute>().FirstOrDefault();
                if (ofXAttribute is not null)
                {
                    var paramExpression = Expression.Parameter(typeof(object), nameof(obj));
                    var castExpression = Expression.Convert(paramExpression, obj.GetType());
                    var propExpression = Expression.Property(castExpression, ofXAttribute.PropertyName);
                    var convertToObject = Expression.Convert(propExpression, typeof(object));
                    var expression = Expression.Lambda<Func<object, object>>(convertToObject, paramExpression);
                    var func = expression.Compile();
                    var graph = Graphs.GetOrAdd(obj.GetType(), DependencyGraphBuilder.BuildDependencyGraph);
                    var order = graph.GetPropertyOrder(property);
                    yield return new MappableDataProperty(property, obj, ofXAttribute, func, ofXAttribute.Expression,
                        order);
                    OfXPropertiesCache.TryAdd(property,
                        new MappableDataPropertyCache(ofXAttribute, func, ofXAttribute.Expression, order));
                    continue;
                }

                try
                {
                    if (GeneralHelpers.IsPrimitiveType(property.PropertyType)) continue;
                    var propertyValue = property.GetValue(obj);
                    switch (propertyValue)
                    {
                        case null: continue;
                        case IEnumerable propertyAsEnumerable:
                            EnumerableObject(propertyAsEnumerable, stack);
                            continue;
                    }

                    if (property.PropertyType.IsClass) stack.Push(propertyValue);
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }
    }

    private static void EnumerableObject(IEnumerable propertyValue, Stack<object> stack)
    {
        foreach (var item in propertyValue)
        {
            if (item is null || GeneralHelpers.IsPrimitiveType(item)) continue;
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
            .GroupBy(x => new { AttributeType = x.Attribute.GetType(), x.Order })
            .Join(ofXAttributeTypes, d => d.Key.AttributeType, x => x,
                (d, x) => new MappableTypeData(x, d
                        .Select(a => new RuntimePropertyCalling(a.Model, a.Func)),
                    d.Select(a => a.Expression), d.Key.Order));

    internal static void MapResponseData(IEnumerable<MappableDataProperty> mappableProperties,
        IEnumerable<(Type OfXAttributeType, ItemsResponse<OfXDataResponse> ItemsResponse)> dataFetched)
    {
        var dataWithExpression = dataFetched
            .Select(a => a.ItemsResponse.Items
                .Select(x => (x.Id, x.OfXValues))
                .Select(k => (a.OfXAttributeType, Data: k)))
            .SelectMany(x => x);
        mappableProperties.Join(dataWithExpression, ap => (ap.Attribute.GetType(), ap.Func
                .Invoke(ap.Model)?.ToString()),
            dt => (dt.OfXAttributeType, dt.Data.Id), (ap, dt) =>
            {
                var value = dt.Data
                    .OfXValues
                    .FirstOrDefault(a => a.Expression == ap.Expression)?.Value;
                if (value is null || ap.PropertyInfo is not { } propertyInfo) return value;
                try
                {
                    var valueSet = SerializeObjects.DeserializeObject(value, propertyInfo.PropertyType);
                    ap.PropertyInfo.SetValue(ap.Model, valueSet);
                }
                catch (Exception)
                {
                    if (OfXStatics.ThrowIfExceptions) throw;
                    // Ignore this field as well
                }

                return value;
            }).IteratorVoid();
    }
}