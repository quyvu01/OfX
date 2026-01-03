using OfX.Tests.Infrastructure.Builders;
using OfX.Tests.TestData.Models;

namespace OfX.Tests.TestData.Builders;

public class ProvinceBuilder : TestEntityBuilder<Province, ProvinceBuilder>
{
    private static int _idCounter = 1;

    protected override void SetDefaults()
    {
        var id = _idCounter++;
        Entity.Id = $"province-{id}";
        Entity.Name = $"Province {id}";
        Entity.CountryId = "country-1";
        Entity.Cities = [];
    }

    public ProvinceBuilder WithId(string id)
    {
        Entity.Id = id;
        return This();
    }

    public ProvinceBuilder WithName(string name)
    {
        Entity.Name = name;
        return This();
    }

    public ProvinceBuilder WithCountryId(string countryId)
    {
        Entity.CountryId = countryId;
        return This();
    }

    public ProvinceBuilder WithCountry(Country country)
    {
        Entity.Country = country;
        Entity.CountryId = country.Id;
        return This();
    }

    public ProvinceBuilder WithCities(List<City> cities)
    {
        Entity.Cities = cities;
        return This();
    }

    public ProvinceBuilder AddCity(City city)
    {
        Entity.Cities.Add(city);
        return This();
    }

    public static Province California() => new ProvinceBuilder()
        .WithId("california")
        .WithName("California")
        .WithCountryId("usa")
        .Build();

    public static Province Texas() => new ProvinceBuilder()
        .WithId("texas")
        .WithName("Texas")
        .WithCountryId("usa")
        .Build();
}
