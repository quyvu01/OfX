using System.Collections;
using OfX.ApplicationModels;
using OfX.MetadataCache;
using OfX.Extensions;
using OfX.Responses;
using OfX.Serializable;
using OfX.Configuration;

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
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        return GetResolvablePropertiesRecursive(rootObject, visited);
    }

    private static IEnumerable<PropertyDescriptor> GetResolvablePropertiesRecursive(object obj, HashSet<object> visited)
    {
        if (obj.IsNullOrPrimitive()) yield break;

        if (obj is IEnumerable enumerable)
        {
            foreach (var item in enumerable is IDictionary dictionary ? dictionary.Values : enumerable)
            foreach (var prop in GetResolvablePropertiesRecursive(item, visited))
                yield return prop;
            yield break;
        }

        if (!visited.Add(obj)) yield break;

        var objType = obj.GetType();
        var objectCached = OfXModelCache.GetModelAccessor(objType);
        foreach (var (propertyInfo, accessor) in objectCached.Accessors)
        {
            var propertyInformation = objectCached.GetInformation(propertyInfo);
            if (propertyInformation.RequiredAccessor is not null)
            {
                yield return new PropertyDescriptor(propertyInfo, obj, propertyInformation);
                continue;
            }

            var propValue = accessor.Get(obj);
            foreach (var value in GetResolvablePropertiesRecursive(propValue, visited))
                yield return value;
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
                        .Select(a => new PropertyMappingData(a.Model, a.PropertyInformation)), d.Key.Order));

    internal static void MapResponseData(IEnumerable<PropertyDescriptor> mappableProperties,
        IEnumerable<(Type OfXAttributeType, ItemsResponse<DataResponse> ItemsResponse)> dataFetched)
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
                    var valueSet = OfXJsonSerializer.DeserializeObject(value, propertyInfo.PropertyType);
                    var modelAccessor = OfXModelCache.GetModelAccessor(ap.Model.GetType());
                    var propertyAccessor = modelAccessor.GetAccessor(ap.PropertyInfo);
                    propertyAccessor?.Set(ap.Model, valueSet);
                }
                catch (Exception)
                {
                    if (OfXStatics.ThrowIfExceptions) throw;
                }

                return value;
            }).Evaluate();
    }
}