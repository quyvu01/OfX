using System.Collections;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using OfX.Abstractions;
using OfX.ApplicationModels;
using OfX.Extensions;
using OfX.Responses;
using OfX.Serializable;

namespace OfX.Helpers;

internal static class ReflectionHelpers
{
    private static readonly ConcurrentDictionary<PropertyInfo, MappableDataPropertyCache>
        OfXPropertiesCache = new();

    internal static IEnumerable<MappableDataProperty> GetMappableProperties(object rootObject)
    {
        if (rootObject is null) yield break;
        var stack = new Stack<object>();
        var rootType = rootObject.GetType();
        if (typeof(IEnumerable).IsAssignableFrom(rootType) && rootType != typeof(string))
        {
            EnumerableObject(rootObject, stack);
            goto startWithStack;
        }

        stack.Push(rootObject);
        startWithStack:
        while (stack.Count > 0)
        {
            var currentObject = stack.Pop();
            if (currentObject == null) continue;
            var currentType = currentObject.GetType();
            var properties = currentType.GetProperties();
            foreach (var property in properties)
            {
                if (OfXPropertiesCache.TryGetValue(property, out var propertyCache))
                {
                    yield return new MappableDataProperty(property, currentObject, propertyCache.Attribute,
                        propertyCache.Func, propertyCache.Expression, propertyCache.Order);
                    continue;
                }

                var ofXAttribute = property.GetCustomAttributes(true)
                    .OfType<IOfXAttributeCore>()
                    .FirstOrDefault();
                if (ofXAttribute is not null && Attribute.IsDefined(property, ofXAttribute.GetType()))
                {
                    var paramExpression = Expression.Parameter(currentObject.GetType());
                    var propExpression = Expression.Property(paramExpression, ofXAttribute.PropertyName);
                    var expression = Expression.Lambda(propExpression, paramExpression);
                    var func = expression.Compile();
                    yield return new MappableDataProperty(property, currentObject, ofXAttribute, func,
                        ofXAttribute.Expression, ofXAttribute.Order);
                    OfXPropertiesCache.TryAdd(property,
                        new MappableDataPropertyCache(ofXAttribute, func, ofXAttribute.Expression,
                            ofXAttribute.Order));
                    continue;
                }

                try
                {
                    if (currentObject is IEnumerable and not string)
                    {
                        EnumerableObject(currentObject, stack);
                        continue;
                    }

                    var propertyValue = property.GetValue(currentObject);
                    if (propertyValue == null) continue;
                    if (propertyValue is IEnumerable and not string)
                    {
                        EnumerableObject(propertyValue, stack);
                        continue;
                    }

                    if (property.PropertyType.IsClass && property.PropertyType != typeof(string))
                        stack.Push(propertyValue);
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }
    }

    private static void EnumerableObject(object propertyValue, Stack<object> stack)
    {
        foreach (var item in (IEnumerable)propertyValue)
            if (item != null && !stack.Contains(item) && item.GetType() != typeof(string))
                stack.Push(item);
    }

    // To use merge-expression, we have to group by attribute only, exclude expression as the older version!
    internal static IEnumerable<MappableTypeData> GetOfXTypesData(
        IEnumerable<MappableDataProperty> datas, IEnumerable<Type> ofXAttributeTypes) =>
        datas
            .GroupBy(x => new { AttributeType = x.Attribute.GetType(), x.Order })
            .Join(ofXAttributeTypes, d => d.Key.AttributeType, x => x,
                (d, x) => new MappableTypeData(x, d
                        .Select(a => new PropertyCalledLater(a.Model, a.Func)),
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
                    // In case when we cannot know the response type, and we accept this is a string, just save the serializable data to a string. Self handle!
                    if (ap.PropertyInfo.PropertyType == typeof(string))
                        ap.PropertyInfo.SetValue(ap.Model, value);
                }

                return value;
            }).IteratorVoid();
    }
}