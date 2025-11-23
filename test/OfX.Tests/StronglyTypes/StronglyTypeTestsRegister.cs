using OfX.Abstractions;

namespace OfX.Tests.StronglyTypes;

public sealed class StronglyTypeTestsRegister :
    IStronglyTypeConverter<UserId>,
    IStronglyTypeConverter<ProvinceId>
{
    UserId IStronglyTypeConverter<UserId>.Convert(string input) => new(Guid.Parse(input));

    bool IStronglyTypeConverter<UserId>.CanConvert(string input) => Guid.TryParse(input, out _);

    ProvinceId IStronglyTypeConverter<ProvinceId>.Convert(string input) => new(Guid.Parse(input));

    bool IStronglyTypeConverter<ProvinceId>.CanConvert(string input) => Guid.TryParse(input, out _);
}