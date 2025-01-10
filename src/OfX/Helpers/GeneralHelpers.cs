using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using OfX.Abstractions;
using OfX.Exceptions;

namespace OfX.Helpers;

public static class GeneralHelpers
{
    private static readonly Lazy<ConcurrentDictionary<Type, (MethodInfo ConvertMethod, MethodInfo CanConvertMethod)>>
        StronglyTypeMethodInfoStorage = new(() =>
            new ConcurrentDictionary<Type, (MethodInfo ConvertMethod, MethodInfo CanConvertMethod)>());

    public static string GetAssemblyName(this Type type) => $"{type.FullName},{type.Assembly.GetName().Name}";

    public static ConstantExpression ConstantExpression(IServiceProvider serviceProvider, List<string> selectorIds,
        Type idType)
    {
        if (idType == typeof(string)) return ParseStrings(selectorIds);

        if (idType == typeof(Guid)) return ParseGuids(selectorIds);

        if (idType == typeof(Guid?)) return ParseNullableGuids(selectorIds);

        if (idType == typeof(int)) return ParseIntegers(selectorIds);

        if (idType == typeof(int?)) return ParseNullableIntegers(selectorIds);

        if (idType == typeof(long)) return ParseLongs(selectorIds);

        if (idType == typeof(long?)) return ParseNullableLongs(selectorIds);

        if (idType == typeof(short)) return ParseShorts(selectorIds);

        if (idType == typeof(short?)) return ParseNullableShorts(selectorIds);

        if (idType == typeof(ulong)) return ParseULongs(selectorIds);

        if (idType == typeof(ulong?)) return ParseNullableULongs(selectorIds);

        if (idType == typeof(ushort)) return ParseUShorts(selectorIds);

        if (idType == typeof(ushort?)) return ParseNullableUShorts(selectorIds);

        if (idType == typeof(byte)) return ParseBytes(selectorIds);

        if (idType == typeof(byte?)) return ParseNullableBytes(selectorIds);

        if (idType == typeof(sbyte)) return ParseSBytes(selectorIds);

        if (idType == typeof(sbyte?)) return ParseNullableSBytes(selectorIds);
        return ParseStronglyTypes(serviceProvider, selectorIds, idType);
    }

    private static ConstantExpression ParseStrings(List<string> selectorIds) => Expression.Constant(selectorIds);

    private static ConstantExpression ParseGuids(List<string> selectorIds) =>
        Expression.Constant(selectorIds
            .Where(a => Guid.TryParse(a, out _))
            .Select(Guid.Parse)
            .ToList());

    private static ConstantExpression ParseIntegers(List<string> selectorIds) =>
        Expression.Constant(selectorIds
            .Where(a => int.TryParse(a, out _))
            .Select(int.Parse)
            .ToList());

    private static ConstantExpression ParseLongs(List<string> selectorIds) =>
        Expression.Constant(selectorIds
            .Where(a => long.TryParse(a, out _))
            .Select(long.Parse)
            .ToList());

    private static ConstantExpression ParseShorts(List<string> selectorIds) =>
        Expression.Constant(selectorIds
            .Where(a => short.TryParse(a, out _))
            .Select(short.Parse)
            .ToList());

    private static ConstantExpression ParseULongs(List<string> selectorIds) =>
        Expression.Constant(selectorIds
            .Where(a => ulong.TryParse(a, out _))
            .Select(ulong.Parse)
            .ToList());

    private static ConstantExpression ParseUShorts(List<string> selectorIds) =>
        Expression.Constant(selectorIds
            .Where(a => ushort.TryParse(a, out _))
            .Select(ushort.Parse)
            .ToList());

    private static ConstantExpression ParseBytes(List<string> selectorIds) =>
        Expression.Constant(selectorIds
            .Where(a => byte.TryParse(a, out _))
            .Select(byte.Parse)
            .ToList());

    private static ConstantExpression ParseSBytes(List<string> selectorIds) =>
        Expression.Constant(selectorIds
            .Where(a => sbyte.TryParse(a, out _))
            .Select(sbyte.Parse)
            .ToList());

    // Parsing methods for nullable types
    private static ConstantExpression ParseNullableGuids(List<string> selectorIds) =>
        Expression.Constant(selectorIds
            .Where(a => Guid.TryParse(a, out _))
            .Select(a => (Guid?)Guid.Parse(a))
            .ToList());

    private static ConstantExpression ParseNullableIntegers(List<string> selectorIds) =>
        Expression.Constant(selectorIds
            .Where(a => int.TryParse(a, out _))
            .Select(a => (int?)int.Parse(a))
            .ToList());

    private static ConstantExpression ParseNullableLongs(List<string> selectorIds) =>
        Expression.Constant(selectorIds
            .Where(a => long.TryParse(a, out _))
            .Select(a => (long?)long.Parse(a))
            .ToList());

    private static ConstantExpression ParseNullableShorts(List<string> selectorIds) =>
        Expression.Constant(selectorIds
            .Where(a => short.TryParse(a, out _))
            .Select(a => (short?)short.Parse(a))
            .ToList());

    private static ConstantExpression ParseNullableULongs(List<string> selectorIds) =>
        Expression.Constant(selectorIds
            .Where(a => ulong.TryParse(a, out _))
            .Select(a => (ulong?)ulong.Parse(a))
            .ToList());

    private static ConstantExpression ParseNullableUShorts(List<string> selectorIds) =>
        Expression.Constant(selectorIds
            .Where(a => ushort.TryParse(a, out _))
            .Select(a => (ushort?)ushort.Parse(a))
            .ToList());

    private static ConstantExpression ParseNullableBytes(List<string> selectorIds) =>
        Expression.Constant(selectorIds
            .Where(a => byte.TryParse(a, out _))
            .Select(a => (byte?)byte.Parse(a))
            .ToList());

    private static ConstantExpression ParseNullableSBytes(List<string> selectorIds) =>
        Expression.Constant(selectorIds
            .Where(a => sbyte.TryParse(a, out _))
            .Select(a => (sbyte?)sbyte.Parse(a))
            .ToList());

    private static ConstantExpression ParseStronglyTypes(IServiceProvider serviceProvider, List<string> selectorIds,
        Type idType)
    {
        // This is temporary for test, we have to add cache and using Expression to cache the data!
        var serviceType = typeof(IStronglyTypeConverter<>).MakeGenericType(idType);
        var stronglyTypeService = serviceProvider.GetService(typeof(IStronglyTypeConverter<>).MakeGenericType(idType));
        if (stronglyTypeService is null)
            throw new OfXException.CurrentIdTypeWasNotSupported();
        var genericMethods = StronglyTypeMethodInfoStorage.Value.GetOrAdd(idType, _ =>
        {
            var methods = serviceType.GetMethods();
            var convertMethod = methods.FirstOrDefault(m =>
                m.Name == "Convert" && m.GetParameters() is { Length: 1 } parameters &&
                parameters[0].ParameterType == typeof(string));
            var canConvertMethod = methods.FirstOrDefault(m =>
                m.Name == "CanConvert" && m.GetParameters() is { Length: 1 } parameters &&
                parameters[0].ParameterType == typeof(string));
            return (convertMethod, canConvertMethod);
        });
        var idsConverted = selectorIds.Where(a => (bool)genericMethods.CanConvertMethod
                .Invoke(stronglyTypeService, [a])!)
            .Select(a => genericMethods.ConvertMethod.Invoke(stronglyTypeService, [a]))
            .ToList();
        return Expression.Constant(idsConverted);
    }
}