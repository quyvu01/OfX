using System.Collections;
using OfX.ApplicationModels;
using OfX.Cached;
using OfX.Extensions;
using OfX.Responses;
using OfX.Serializable;
using OfX.Statics;

namespace OfX.Helpers;

/// <summary>
/// Provides reflection-based helper methods for discovering and mapping OfX-decorated properties.
/// </summary>
/// <remarks>
/// This class handles the core reflection logic for:
/// <list type="bullet">
///   <item><description>Discovering properties with OfX attributes on objects</description></item>
///   <item><description>Grouping properties by attribute type and execution order</description></item>
///   <item><description>Mapping response data back to object properties</description></item>
/// </list>
/// </remarks>
internal static class ReflectionHelpers
{
    internal static IEnumerable<PropertyDescriptor> DiscoverResolvableProperties(object rootObject)
    {
        if (rootObject.IsNullOrPrimitive()) yield break;
        Stack<object> stack = [];
        ObjectProcessing(rootObject, stack);

        while (stack.TryPop(out var obj))
            foreach (var mappableDataProperty in GetResolvablePropertiesRecursive(obj, stack))
                yield return mappableDataProperty;
    }

    private static IEnumerable<PropertyDescriptor> GetResolvablePropertiesRecursive(object obj, Stack<object> stack)
    {
        if (obj.IsNullOrPrimitive()) yield break;
        if (obj is IEnumerable enumerable)
        {
            EnumerableObject(enumerable, stack);
            yield break;
        }

        var objType = obj.GetType();
        var objectCached = OfXModelCache.GetModel(objType);
        foreach (var (propertyInfo, accessor) in objectCached.Accessors)
        {
            var propertyInformation = objectCached.GetInformation(propertyInfo);
            if (propertyInformation.RequiredAccessor is not null)
            {
                yield return new PropertyDescriptor(propertyInfo, obj, propertyInformation);
                continue;
            }

            var propValue = accessor.Get(obj);
            if (propValue.IsNullOrPrimitive()) continue;
            foreach (var value in GetResolvablePropertiesRecursive(propValue, stack)) yield return value;
        }
    }

    // private static bool InvalidObject(object obj) => obj is null || GeneralHelpers.IsPrimitiveType(obj);

    private static void EnumerableObject(IEnumerable enumerableObject, Stack<object> stack)
    {
        if (enumerableObject is not IDictionary dictionary)
        {
            foreach (var item in enumerableObject) ObjectProcessing(item, stack);
            return;
        }

        foreach (var value in dictionary.Values) ObjectProcessing(value, stack);
    }

    private static void ObjectProcessing(object obj, Stack<object> stack)
    {
        if (obj.IsNullOrPrimitive()) return;
        switch (obj)
        {
            case IEnumerable enumerable:
                EnumerableObject(enumerable, stack);
                break;
            default:
                if (!stack.Contains(obj)) stack.Push(obj);
                break;
        }
    }

    // To use merge-expression, we have to group by attribute only, exclude expression as the older version!
    internal static IEnumerable<AttributeTypeInfo> GetOfXTypesData
        (IEnumerable<PropertyDescriptor> mappableDataProperties, IEnumerable<Type> attributeTypes) =>
        mappableDataProperties
            .GroupBy(mdp => (AttributeType: mdp.PropertyInformation?.RuntimeAttributeType,
                Order: mdp.PropertyInformation?.Order ?? 0))
            .Join(attributeTypes, gr => gr.Key.AttributeType, at => at,
                (d, x) =>
                    new AttributeTypeInfo(x, d
                        .Select(a => new PropertyAssessorData(a.Model, a.PropertyInformation)), d.Key.Order));

    internal static void MapResponseData(IEnumerable<PropertyDescriptor> mappableProperties,
        IEnumerable<(Type OfXAttributeType, ItemsResponse<OfXDataResponse> ItemsResponse)> dataFetched)
    {
        var dataWithExpression = dataFetched
            .Select(a => a.ItemsResponse.Items
                .Select(x => (x.Id, x.OfXValues))
                .Select(k => (a.OfXAttributeType, Data: k)))
            .SelectMany(x => x);
        mappableProperties.Join(dataWithExpression, ap => (ap.PropertyInformation?.RuntimeAttributeType, ap
                .PropertyInformation?
                .RequiredAccessor?
                .Get(ap.Model)?.ToString()),
            dt => (dt.OfXAttributeType, dt.Data.Id), (ap, dt) =>
            {
                var value = dt.Data
                    .OfXValues
                    .FirstOrDefault(a => a.Expression == ap.PropertyInformation.Expression)?.Value;
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
            }).Evaluate();
    }
}