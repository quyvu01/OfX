using Microsoft.EntityFrameworkCore;
using Service3Api.Contexts;
using Service3Api.Models;

namespace Service3Api.Data;

/// <summary>
/// Provides seed data for Service3 (Countries and Provinces).
/// This is the foundation data that other services depend on via ProvinceId FK.
/// </summary>
public static class Service3DataSeeder
{
    private static readonly Dictionary<string, (string Name, List<(Guid Id, string Name)> Provinces)> CountryData = new()
    {
        {
            "US", ("United States", [
                (Guid.Parse("01962f9a-f7f8-7f61-941c-6a086fe96cd2"), "California"),
                (Guid.Parse("01962f9a-f7f8-7b4c-9b4d-eae8ea6e5fc7"), "New York"),
                (Guid.Parse("01962f9a-f7f8-7e54-a79d-575a8e882eb8"), "Texas"),
                (Guid.Parse("01962f9a-f7f8-7a2c-8b3d-4c5e6f789012"), "Florida"),
                (Guid.Parse("01962f9a-f7f8-7d1b-9c2a-5d6e7f890123"), "Illinois")
            ])
        },
        {
            "CA", ("Canada", [
                (Guid.Parse("01962f9a-f7f8-7c3d-8e4f-6a7b8c9d0123"), "Ontario"),
                (Guid.Parse("01962f9a-f7f8-7e5f-9a6b-7c8d9e0f1234"), "Quebec"),
                (Guid.Parse("01962f9a-f7f8-7f6a-8b7c-9d0e1f234567"), "British Columbia")
            ])
        },
        {
            "UK", ("United Kingdom", [
                (Guid.Parse("01962f9a-f7f8-7a7b-9c8d-0e1f23456789"), "England"),
                (Guid.Parse("01962f9a-f7f8-7b8c-0d9e-1f2345678901"), "Scotland"),
                (Guid.Parse("01962f9a-f7f8-7c9d-1e0f-23456789012a"), "Wales")
            ])
        },
        {
            "AU", ("Australia", [
                (Guid.Parse("01962f9a-f7f8-7d0e-2f1a-3456789012bc"), "New South Wales"),
                (Guid.Parse("01962f9a-f7f8-7e1f-3a2b-456789012cde"), "Victoria"),
                (Guid.Parse("01962f9a-f7f8-7f2a-4b3c-56789012def0"), "Queensland")
            ])
        },
        {
            "DE", ("Germany", [
                (Guid.Parse("01962f9a-f7f8-7a3b-5c4d-6789012ef012"), "Bavaria"),
                (Guid.Parse("01962f9a-f7f8-7b4c-6d5e-789012f01234"), "North Rhine-Westphalia"),
                (Guid.Parse("01962f9a-f7f8-7c5d-7e6f-89012f012345"), "Baden-Württemberg")
            ])
        },
        {
            "FR", ("France", [
                (Guid.Parse("01962f9a-f7f8-7d6e-8f7a-9012f0123456"), "Île-de-France"),
                (Guid.Parse("01962f9a-f7f8-7e7f-9a8b-012f01234567"), "Provence-Alpes-Côte d'Azur"),
                (Guid.Parse("01962f9a-f7f8-7f8a-0b9c-12f012345678"), "Nouvelle-Aquitaine")
            ])
        },
        {
            "JP", ("Japan", [
                (Guid.Parse("01962f9a-f7f8-7a9b-1c0d-2f01234567ab"), "Tokyo"),
                (Guid.Parse("01962f9a-f7f8-7b0c-2d1e-f01234567abc"), "Osaka"),
                (Guid.Parse("01962f9a-f7f8-7c1d-3e2f-01234567abcd"), "Hokkaido")
            ])
        },
        {
            "VN", ("Vietnam", [
                (Guid.Parse("01962f9a-f7f8-7d2e-4f3a-1234567abcde"), "Ho Chi Minh City"),
                (Guid.Parse("01962f9a-f7f8-7e3f-5a4b-234567abcdef"), "Hanoi"),
                (Guid.Parse("01962f9a-f7f8-7f4a-6b5c-34567abcdef0"), "Da Nang")
            ])
        }
    };

    public static async Task SeedAsync(Service3Context context, CancellationToken cancellationToken = default)
    {
        var countrySet = context.Set<Country>();

        foreach (var (countryId, (countryName, provinces)) in CountryData)
        {
            var existedCountry = await countrySet
                .Include(c => c.Provinces)
                .FirstOrDefaultAsync(x => x.Id == countryId, cancellationToken);

            if (existedCountry == null)
            {
                countrySet.Add(new Country
                {
                    Id = countryId,
                    Name = countryName,
                    Provinces = provinces.Select(p => new Province
                    {
                        Id = p.Id,
                        Name = p.Name
                    }).ToList()
                });
            }
            else
            {
                // Update provinces if needed
                var existingProvinceIds = existedCountry.Provinces.Select(p => p.Id).ToHashSet();
                var newProvinces = provinces
                    .Where(p => !existingProvinceIds.Contains(p.Id))
                    .Select(p => new Province
                    {
                        Id = p.Id,
                        Name = p.Name,
                        CountryId = countryId
                    });

                context.Set<Province>().AddRange(newProvinces);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Gets all province IDs for use in other services.
    /// </summary>
    public static List<Guid> GetAllProvinceIds()
    {
        return CountryData
            .SelectMany(c => c.Value.Provinces.Select(p => p.Id))
            .ToList();
    }

    /// <summary>
    /// Gets province IDs for a specific country.
    /// </summary>
    public static List<Guid> GetProvinceIdsByCountry(string countryId)
    {
        return CountryData.TryGetValue(countryId, out var countryData)
            ? countryData.Provinces.Select(p => p.Id).ToList()
            : [];
    }
}
