using System.Collections;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using OfX.Abstractions;
using OfX.ApplicationModels;
using OfX.Extensions;
using OfX.ObjectContexts;
using OfX.Responses;
using OfX.Serializable;

namespace OfX.Helpers;

internal static class ReflectionHelpers
{
    private static readonly ConcurrentDictionary<PropertyInfo, MappableDataPropertyCache>
        OfXPropertiesCache = new();

    private static readonly ConcurrentDictionary<Type, Dictionary<PropertyInfo, PropertyContext[]>> Graphs = [];

    internal static IEnumerable<MappableDataProperty> GetMappableProperties(object rootObject)
    {
        if (rootObject is null or string) yield break;
        Stack<object> stack = [];
        if (rootObject is IEnumerable rootObjectAsEnumerable)
        {
            EnumerableObject(rootObjectAsEnumerable, stack);
            goto startWithStack;
        }

        stack.Push(rootObject);
        startWithStack:
        while (stack.Count > 0)
        {
            var obj = stack.Pop();
            if (obj is null) continue;
            var objType = obj.GetType();
            if (Graphs.TryGetValue(objType, out var graphs))
            {
                foreach (var mappableDataProperty in IterateMappableProperties([..graphs.Keys], obj, objType))
                    yield return mappableDataProperty;
                continue;
            }


            var properties = objType.GetProperties();
            foreach (var mappableDataProperty in IterateMappableProperties(properties, obj, objType))
                yield return mappableDataProperty;
        }

        yield break;

        IEnumerable<MappableDataProperty> IterateMappableProperties(PropertyInfo[] properties, object obj, Type objType)
        {
            if (obj is null or string) yield break;
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

                var ofXAttribute = property.GetCustomAttributes(true)
                    .OfType<IOfXAttributeCore>()
                    .FirstOrDefault();
                if (ofXAttribute is not null && Attribute.IsDefined(property, ofXAttribute.GetType()))
                {
                    var paramExpression = Expression.Parameter(typeof(object), nameof(obj));
                    var castExpression = Expression.Convert(paramExpression, obj.GetType());
                    var propExpression = Expression.Property(castExpression, ofXAttribute.PropertyName);
                    var convertToObject = Expression.Convert(propExpression, typeof(object));
                    var expression = Expression.Lambda<Func<object, object>>(convertToObject, paramExpression);
                    var func = expression.Compile();
                    var graph = Graphs.GetOrAdd(objType, DependencyGraphBuilder.BuildDependencyGraph);
                    var order = graph.GetPropertyOrder(property);
                    yield return new MappableDataProperty(property, obj, ofXAttribute, func, ofXAttribute.Expression,
                        order);
                    OfXPropertiesCache.TryAdd(property,
                        new MappableDataPropertyCache(ofXAttribute, func, ofXAttribute.Expression, order));
                    continue;
                }

                try
                {
                    var propertyValue = property.GetValue(obj);
                    if (propertyValue is null or string) continue;
                    if (propertyValue is IEnumerable propertyAsEnumerable)
                    {
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
            if (item is null or string) continue;
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

    internal static void MapResponseData(IEnumerable<MappableDataProperty> allPropertyDatas,
        IEnumerable<(Type OfXAttributeType, ItemsResponse<OfXDataResponse> ItemsResponse)> dataFetched)
    {
        var dataWithExpression = dataFetched
            .Select(a => a.ItemsResponse.Items
                .Select(x => (x.Id, x.OfXValues))
                .Select(k => (a.OfXAttributeType, Data: k)))
            .SelectMany(x => x);
        allPropertyDatas.Join(dataWithExpression, ap => (ap.Attribute.GetType(), ap.Func
                .DynamicInvoke(ap.Model)?.ToString()),
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
                    // In case when we cannot know the response type, and we accept this is a string, just save the serializable data to a string. Self-handle!
                    if (ap.PropertyInfo.PropertyType == typeof(string))
                        ap.PropertyInfo.SetValue(ap.Model, value);
                }

                return value;
            }).IteratorVoid();
    }
}