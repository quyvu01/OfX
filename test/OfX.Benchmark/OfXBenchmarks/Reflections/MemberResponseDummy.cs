using OfX.Benchmark.OfXBenchmarks.Reflections.Models;

namespace OfX.Benchmark.OfXBenchmarks.Reflections;

public static class MixedDummyFactory
{
    private static readonly Random Rand = new();

    public static object[] CreateDummyMixed(int count = 200)
    {
        var result = new object[count];

        for (int i = 0; i < count; i++)
        {
            result[i] = Rand.Next(6) switch
            {
                0 => CreateMember(i),
                1 => CreateOrder(i),
                2 => CreateProduct(i),
                3 => CreateProvince(i),
                4 => CreateCountry(i),
                _ => CreateAudit(i)
            };
        }

        return result;
    }

    private static MemberResponse CreateMember(int i)
    {
        return new MemberResponse
        {
            Id = $"m-{i}",
            UserId = $"user-{i}",
            UserName = $"User {i}",
            UserEmail = $"user{i}@mail.com",
            UserCustomExpression = $"custom-{i}",

            ProvinceId = $"province-{i % 50}",
            ProvinceName = $"Province {i % 50}",

            CountryId = $"country-{i % 20}",
            CountryName = $"Country {i % 20}",

            Provinces =
            [
                new ProvinceResponse
                {
                    Id = $"province-{i % 50}",
                    CountryId = $"country-{i % 20}",
                }
            ],

            Province = new ProvinceResponse
            {
                Id = $"province-{i % 50}",
                CountryId = $"country-{i % 20}",
            }
        };
    }

    private static OrderResponse CreateOrder(int i)
    {
        return new OrderResponse
        {
            Id = $"o-{i}",
            UserId = $"user-{i}",
            UserName = $"User {i}",
            TotalAmount = Rand.Next(10, 5000),

            CountryId = $"country-{i % 20}",
            CountryName = $"Country {i % 20}"
        };
    }

    private static ProductResponse CreateProduct(int i)
    {
        return new ProductResponse
        {
            Id = $"p-{i}",
            SellerId = $"seller-{i}",
            SellerName = $"Seller {i}"
        };
    }

    private static ProvinceResponse CreateProvince(int i)
    {
        return new ProvinceResponse
        {
            Id = $"province-{i}",
            CountryId = $"country-{i % 20}",
            CountryName = $"Country {i % 20}"
        };
    }

    private static CountryResponse CreateCountry(int i)
    {
        return new CountryResponse
        {
            Id = $"country-{i}",
            Name = $"Country {i}"
        };
    }

    private static AuditLogResponse CreateAudit(int i)
    {
        return new AuditLogResponse
        {
            Id = $"a-{i}",
            ActorId = $"user-{i}",
            ActorName = $"User {i}"
        };
    }
}