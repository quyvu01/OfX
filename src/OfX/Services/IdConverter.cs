#nullable enable
using Microsoft.Extensions.DependencyInjection;
using OfX.Abstractions;
using OfX.Exceptions;

namespace OfX.Services;

internal abstract class AbstractIdConverter
{
    protected static readonly Dictionary<Type, Func<string[], object>> IdConverters = new()
    {
        { typeof(string), ids => ids },
        { typeof(Guid), ParseIdsToArray<Guid> },
        { typeof(Guid?), ParseNullableIdsToArray<Guid> },
        { typeof(int), ParseIdsToArray<int> },
        { typeof(int?), ParseNullableIdsToArray<int> },
        { typeof(long), ParseIdsToArray<long> },
        { typeof(long?), ParseNullableIdsToArray<long> },
        { typeof(ulong), ParseIdsToArray<ulong> },
        { typeof(ulong?), ParseNullableIdsToArray<ulong> },
        { typeof(short), ParseIdsToArray<short> },
        { typeof(short?), ParseNullableIdsToArray<short> },
        { typeof(ushort), ParseIdsToArray<ushort> },
        { typeof(ushort?), ParseNullableIdsToArray<ushort> },
        { typeof(float), ParseIdsToArray<float> },
        { typeof(float?), ParseNullableIdsToArray<float> },
        { typeof(double), ParseIdsToArray<double> },
        { typeof(double?), ParseNullableIdsToArray<double> },
        { typeof(decimal), ParseIdsToArray<decimal> },
        { typeof(decimal?), ParseNullableIdsToArray<decimal> },
        { typeof(sbyte), ParseIdsToArray<sbyte> },
        { typeof(sbyte?), ParseNullableIdsToArray<sbyte> },
        { typeof(uint), ParseIdsToArray<uint> },
        { typeof(uint?), ParseNullableIdsToArray<uint> },
        { typeof(byte), ParseIdsToArray<byte> },
        { typeof(byte?), ParseNullableIdsToArray<byte> }
    };

    private static object ParseIdsToArray<T>(string[] selectorIds) where T : IParsable<T> =>
        ParseIds<T>(selectorIds).ToArray();

    private static object ParseNullableIdsToArray<T>(string[] selectorIds) where T : IParsable<T>
        => ParseNullableIds<T>(selectorIds).ToArray();

    private static IEnumerable<T> ParseIds<T>(string[] selectorIds)
        where T : IParsable<T>
    {
        if (selectorIds is not { Length: > 0 }) yield break;
        foreach (var id in selectorIds)
            if (T.TryParse(id, null, out var parsed))
                yield return parsed;
    }

    private static IEnumerable<T?> ParseNullableIds<T>(string[] selectorIds)
        where T : IParsable<T>
    {
        if (selectorIds is not { Length: > 0 }) yield break;
        foreach (var id in selectorIds)
            if (T.TryParse(id, null, out var parsed))
                yield return parsed;
    }

    protected static IEnumerable<TId> ParseStronglyTypeIds<TId>(IServiceProvider serviceProvider, string[] selectorIds)
    {
        var stronglyTypeService = serviceProvider.GetService<IStronglyTypeConverter<TId>>();
        if (stronglyTypeService is null) throw new OfXException.CurrentIdTypeWasNotSupported();
        return selectorIds
            .Where(a => stronglyTypeService.CanConvert(a))
            .Select(a => stronglyTypeService.Convert(a));
    }
}

internal class IdConverter<TId>(IServiceProvider serviceProvider) : AbstractIdConverter, IIdConverter<TId>
{
    public object ConvertIds(string[] selectorIds) => IdConverters.TryGetValue(typeof(TId), out var converter)
        ? converter(selectorIds)
        : ParseStronglyTypeIds<TId>(serviceProvider, selectorIds);
}