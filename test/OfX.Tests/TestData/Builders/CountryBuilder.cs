using OfX.Tests.Infrastructure.Builders;
using OfX.Tests.TestData.Models;

namespace OfX.Tests.TestData.Builders;

public class CountryBuilder : TestEntityBuilder<Country, CountryBuilder>
{
    private static int _idCounter = 1;

    protected override void SetDefaults()
    {
        var id = _idCounter++;
        Entity.Id = $"country-{id}";
        Entity.Name = $"Country {id}";
        Entity.Code = $"C{id}";
        Entity.Provinces = [];
    }

    public CountryBuilder WithId(string id)
    {
        Entity.Id = id;
        return This();
    }

    public CountryBuilder WithName(string name)
    {
        Entity.Name = name;
        return This();
    }

    public CountryBuilder WithCode(string code)
    {
        Entity.Code = code;
        return This();
    }

    public CountryBuilder WithProvinces(List<Province> provinces)
    {
        Entity.Provinces = provinces;
        return This();
    }

    public CountryBuilder AddProvince(Province province)
    {
        Entity.Provinces.Add(province);
        return This();
    }

    public static Country USA() => new CountryBuilder()
        .WithId("usa")
        .WithName("United States")
        .WithCode("US")
        .Build();

    public static Country Canada() => new CountryBuilder()
        .WithId("canada")
        .WithName("Canada")
        .WithCode("CA")
        .Build();
}
