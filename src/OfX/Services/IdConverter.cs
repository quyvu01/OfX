using Microsoft.Extensions.DependencyInjection;
using OfX.Abstractions;
using OfX.Exceptions;

namespace OfX.Services;

internal class IdConverter<TId>(IServiceProvider serviceProvider)
    : IIdConverter<TId>
{
    public object ConvertIds(List<string> selectorIds)
    {
        var idType = typeof(TId);

        if (idType == typeof(string)) return ParseStringIds(selectorIds);

        if (idType == typeof(Guid)) return ParseGuidIds(selectorIds);

        if (idType == typeof(Guid?)) return ParseNullableGuidIds(selectorIds);

        if (idType == typeof(int)) return ParseIntegerIds(selectorIds);

        if (idType == typeof(int?)) return ParseNullableIntegerIds(selectorIds);

        if (idType == typeof(long)) return ParseLongIds(selectorIds);

        if (idType == typeof(long?)) return ParseNullableLongIds(selectorIds);

        if (idType == typeof(short)) return ParseShortIds(selectorIds);

        if (idType == typeof(short?)) return ParseNullableShortIds(selectorIds);

        if (idType == typeof(ulong)) return ParseULongIds(selectorIds);

        if (idType == typeof(ulong?)) return ParseNullableULongIds(selectorIds);

        if (idType == typeof(ushort)) return ParseUShortIds(selectorIds);

        if (idType == typeof(ushort?)) return ParseNullableUShortIds(selectorIds);

        if (idType == typeof(byte)) return ParseByteIds(selectorIds);

        if (idType == typeof(byte?)) return ParseNullableByteIds(selectorIds);

        if (idType == typeof(sbyte)) return ParseSByteIds(selectorIds);

        if (idType == typeof(sbyte?)) return ParseNullableSByteIds(selectorIds);

        // If all the ID type does not match with provided types. We will try to resolve it from IdConverter Service.
        return ParseStronglyTypeIds(serviceProvider, selectorIds);
    }

    #region ConvertIds

    private static List<string> ParseStringIds(List<string> selectorIds) => selectorIds;

    private static List<Guid> ParseGuidIds(List<string> selectorIds) => selectorIds
        .Where(a => Guid.TryParse(a, out _))
        .Select(Guid.Parse)
        .ToList();

    private static List<int> ParseIntegerIds(List<string> selectorIds) => selectorIds
        .Where(a => int.TryParse(a, out _))
        .Select(int.Parse)
        .ToList();

    private static List<long> ParseLongIds(List<string> selectorIds) => selectorIds
        .Where(a => long.TryParse(a, out _))
        .Select(long.Parse)
        .ToList();

    private static List<short> ParseShortIds(List<string> selectorIds) => selectorIds
        .Where(a => short.TryParse(a, out _))
        .Select(short.Parse)
        .ToList();

    private static List<ulong> ParseULongIds(List<string> selectorIds) => selectorIds
        .Where(a => ulong.TryParse(a, out _))
        .Select(ulong.Parse)
        .ToList();

    private static List<ushort> ParseUShortIds(List<string> selectorIds) =>
        selectorIds
            .Where(a => ushort.TryParse(a, out _))
            .Select(ushort.Parse)
            .ToList();

    private static List<byte> ParseByteIds(List<string> selectorIds) => selectorIds
        .Where(a => byte.TryParse(a, out _))
        .Select(byte.Parse)
        .ToList();

    private static List<sbyte> ParseSByteIds(List<string> selectorIds) => selectorIds
        .Where(a => sbyte.TryParse(a, out _))
        .Select(sbyte.Parse)
        .ToList();

    // Parsing methods for nullable types
    private static List<Guid?> ParseNullableGuidIds(List<string> selectorIds) => selectorIds
        .Where(a => Guid.TryParse(a, out _))
        .Select(a => (Guid?)Guid.Parse(a))
        .ToList();

    private static List<int?> ParseNullableIntegerIds(List<string> selectorIds) => selectorIds
        .Where(a => int.TryParse(a, out _))
        .Select(a => (int?)int.Parse(a))
        .ToList();

    private static List<long?> ParseNullableLongIds(List<string> selectorIds) => selectorIds
        .Where(a => long.TryParse(a, out _))
        .Select(a => (long?)long.Parse(a))
        .ToList();

    private static List<short?> ParseNullableShortIds(List<string> selectorIds) => selectorIds
        .Where(a => short.TryParse(a, out _))
        .Select(a => (short?)short.Parse(a))
        .ToList();

    private static List<ulong?> ParseNullableULongIds(List<string> selectorIds) => selectorIds
        .Where(a => ulong.TryParse(a, out _))
        .Select(a => (ulong?)ulong.Parse(a))
        .ToList();

    private static List<ushort?> ParseNullableUShortIds(List<string> selectorIds) => selectorIds
        .Where(a => ushort.TryParse(a, out _))
        .Select(a => (ushort?)ushort.Parse(a))
        .ToList();

    private static List<byte?> ParseNullableByteIds(List<string> selectorIds) => selectorIds
        .Where(a => byte.TryParse(a, out _))
        .Select(a => (byte?)byte.Parse(a))
        .ToList();

    private static List<sbyte?> ParseNullableSByteIds(List<string> selectorIds) => selectorIds
        .Where(a => sbyte.TryParse(a, out _))
        .Select(a => (sbyte?)sbyte.Parse(a))
        .ToList();

    private static List<TId> ParseStronglyTypeIds(IServiceProvider serviceProvider, List<string> selectorIds)
    {
        var stronglyTypeService = serviceProvider.GetService<IStronglyTypeConverter<TId>>();
        if (stronglyTypeService is null) throw new OfXException.CurrentIdTypeWasNotSupported();
        return selectorIds
            .Where(a => stronglyTypeService.CanConvert(a))
            .Select(a => stronglyTypeService.Convert(a))
            .ToList();
    }

    #endregion
}