using OfX.Abstractions;
using Service3Api.ModelIds;

namespace Service3Api.Converters;

public sealed class IdConverterRegister :
    IStronglyTypeConverter<ProvinceId>,
    IStronglyTypeConverter<CountryId>
{
    ProvinceId IStronglyTypeConverter<ProvinceId>.Convert(string input) => new(Guid.Parse(input));

    bool IStronglyTypeConverter<CountryId>.CanConvert(string input) => true;

    CountryId IStronglyTypeConverter<CountryId>.Convert(string input) => new(input);

    bool IStronglyTypeConverter<ProvinceId>.CanConvert(string input) => Guid.TryParse(input, out _);
}