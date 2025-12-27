using OfX.Tests.Infrastructure.Builders;
using OfX.Tests.TestData.Models;

namespace OfX.Tests.TestData.Builders;

public class CityBuilder : TestEntityBuilder<City, CityBuilder>
{
    private static int _idCounter = 1;

    protected override void SetDefaults()
    {
        var id = _idCounter++;
        Entity.Id = $"city-{id}";
        Entity.Name = $"City {id}";
        Entity.ProvinceId = "province-1";
        Entity.Population = 100000 * id;
    }

    public CityBuilder WithId(string id)
    {
        Entity.Id = id;
        return This();
    }

    public CityBuilder WithName(string name)
    {
        Entity.Name = name;
        return This();
    }

    public CityBuilder WithProvinceId(string provinceId)
    {
        Entity.ProvinceId = provinceId;
        return This();
    }

    public CityBuilder WithPopulation(int population)
    {
        Entity.Population = population;
        return This();
    }

    public static City LosAngeles() => new CityBuilder()
        .WithId("los-angeles")
        .WithName("Los Angeles")
        .WithProvinceId("california")
        .WithPopulation(3_900_000)
        .Build();

    public static City SanFrancisco() => new CityBuilder()
        .WithId("san-francisco")
        .WithName("San Francisco")
        .WithProvinceId("california")
        .WithPopulation(800_000)
        .Build();
}
