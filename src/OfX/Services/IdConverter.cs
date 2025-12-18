using System.Globalization;
using System.Numerics;
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


        if (idType == typeof(int)) return ParseNumberIds<int>(selectorIds);
        if (idType == typeof(int?)) return ParseNullableNumberIds<int>(selectorIds);

        if (idType == typeof(long)) return ParseNumberIds<long>(selectorIds);
        if (idType == typeof(long?)) return ParseNullableNumberIds<long>(selectorIds);

        if (idType == typeof(short)) return ParseNumberIds<short>(selectorIds);
        if (idType == typeof(short?)) return ParseNullableNumberIds<short>(selectorIds);

        if (idType == typeof(int)) return ParseNumberIds<int>(selectorIds);
        if (idType == typeof(int?)) return ParseNullableNumberIds<int>(selectorIds);

        if (idType == typeof(long)) return ParseNumberIds<long>(selectorIds);
        if (idType == typeof(long?)) return ParseNullableNumberIds<long>(selectorIds);

        if (idType == typeof(short)) return ParseNumberIds<short>(selectorIds);
        if (idType == typeof(short?)) return ParseNullableNumberIds<short>(selectorIds);

        if (idType == typeof(sbyte)) return ParseNumberIds<sbyte>(selectorIds);
        if (idType == typeof(sbyte?)) return ParseNullableNumberIds<sbyte>(selectorIds);

        if (idType == typeof(uint)) return ParseNumberIds<uint>(selectorIds);
        if (idType == typeof(uint?)) return ParseNullableNumberIds<uint>(selectorIds);

        if (idType == typeof(ulong)) return ParseNumberIds<ulong>(selectorIds);
        if (idType == typeof(ulong?)) return ParseNullableNumberIds<ulong>(selectorIds);

        if (idType == typeof(ushort)) return ParseNumberIds<ushort>(selectorIds);
        if (idType == typeof(ushort?)) return ParseNullableNumberIds<ushort>(selectorIds);

        if (idType == typeof(byte)) return ParseNumberIds<byte>(selectorIds);
        if (idType == typeof(byte?)) return ParseNullableNumberIds<byte>(selectorIds);

        if (idType == typeof(float)) return ParseNumberIds<float>(selectorIds);
        if (idType == typeof(float?)) return ParseNullableNumberIds<float>(selectorIds);

        if (idType == typeof(double)) return ParseNumberIds<double>(selectorIds);
        if (idType == typeof(double?)) return ParseNullableNumberIds<double>(selectorIds);

        if (idType == typeof(decimal)) return ParseNumberIds<decimal>(selectorIds);
        if (idType == typeof(decimal?)) return ParseNullableNumberIds<decimal>(selectorIds);

        // If all the ID type does not match with provided types. We will try to resolve it from IdConverter Service.
        return ParseStronglyTypeIds(serviceProvider, selectorIds);
    }

    #region ConvertIds

    private static List<string> ParseStringIds(List<string> selectorIds) => selectorIds;

    private static List<Guid> ParseGuidIds(List<string> selectorIds) => selectorIds
        .Where(a => Guid.TryParse(a, out _))
        .Select(Guid.Parse)
        .ToList();

    private static List<Guid?> ParseNullableGuidIds(List<string> selectorIds) => selectorIds
        .Where(a => Guid.TryParse(a, out _))
        .Select(a => (Guid?)Guid.Parse(a))
        .ToList();

    private static List<T> ParseNumberIds<T>(List<string> selectorIds)
        where T : INumber<T> => selectorIds
        .Where(a => T.TryParse(a, CultureInfo.InvariantCulture, out _))
        .Select(a => T.Parse(a, CultureInfo.InvariantCulture))
        .ToList();

    private static List<T?> ParseNullableNumberIds<T>(List<string> selectorIds)
        where T : struct, INumber<T> => selectorIds
        .Where(a => T.TryParse(a, CultureInfo.InvariantCulture, out _))
        .Select(a => (T?)T.Parse(a, CultureInfo.InvariantCulture))
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