using Microsoft.EntityFrameworkCore;
using Service2.Contexts;
using Service2.Models;

namespace Service2.Data;

/// <summary>
/// Provides seed data for Service2 (Users).
/// Users are linked to Provinces from Service3 via ProvinceId FK.
/// </summary>
public static class Service2DataSeeder
{
    private static readonly List<(string Id, string Name, string Email, Guid ProvinceId)> UserData =
    [
        ("user-001", "John Smith", "john.smith@email.com", Guid.Parse("01962f9a-f7f8-7f61-941c-6a086fe96cd2")),
        ("user-002", "Sarah Johnson", "sarah.j@email.com", Guid.Parse("01962f9a-f7f8-7f61-941c-6a086fe96cd2")),
        ("user-003", "Michael Brown", "michael.b@email.com", Guid.Parse("01962f9a-f7f8-7f61-941c-6a086fe96cd2")),

        // New York users
        ("user-004", "Emily Davis", "emily.davis@email.com", Guid.Parse("01962f9a-f7f8-7b4c-9b4d-eae8ea6e5fc7")),
        ("user-005", "David Wilson", "david.w@email.com", Guid.Parse("01962f9a-f7f8-7b4c-9b4d-eae8ea6e5fc7")),
        ("user-006", "Jennifer Martinez", "jennifer.m@email.com", Guid.Parse("01962f9a-f7f8-7b4c-9b4d-eae8ea6e5fc7")),

        // Texas users
        ("user-007", "Robert Anderson", "robert.a@email.com", Guid.Parse("01962f9a-f7f8-7e54-a79d-575a8e882eb8")),
        ("user-008", "Lisa Taylor", "lisa.t@email.com", Guid.Parse("01962f9a-f7f8-7e54-a79d-575a8e882eb8")),

        // Florida users
        ("user-009", "James Thomas", "james.t@email.com", Guid.Parse("01962f9a-f7f8-7a2c-8b3d-4c5e6f789012")),
        ("user-010", "Mary Jackson", "mary.j@email.com", Guid.Parse("01962f9a-f7f8-7a2c-8b3d-4c5e6f789012")),

        // Illinois users
        ("user-011", "William White", "william.w@email.com", Guid.Parse("01962f9a-f7f8-7d1b-9c2a-5d6e7f890123")),
        ("user-012", "Patricia Harris", "patricia.h@email.com", Guid.Parse("01962f9a-f7f8-7d1b-9c2a-5d6e7f890123")),

        // Ontario users
        ("user-013", "Daniel Martin", "daniel.m@email.com", Guid.Parse("01962f9a-f7f8-7c3d-8e4f-6a7b8c9d0123")),
        ("user-014", "Linda Thompson", "linda.t@email.com", Guid.Parse("01962f9a-f7f8-7c3d-8e4f-6a7b8c9d0123")),

        // Quebec users
        ("user-015", "Charles Garcia", "charles.g@email.com", Guid.Parse("01962f9a-f7f8-7e5f-9a6b-7c8d9e0f1234")),
        ("user-016", "Barbara Martinez", "barbara.m@email.com", Guid.Parse("01962f9a-f7f8-7e5f-9a6b-7c8d9e0f1234")),

        // British Columbia users
        ("user-017", "Thomas Robinson", "thomas.r@email.com", Guid.Parse("01962f9a-f7f8-7f6a-8b7c-9d0e1f234567")),
        ("user-018", "Susan Clark", "susan.c@email.com", Guid.Parse("01962f9a-f7f8-7f6a-8b7c-9d0e1f234567")),

        // England users
        ("user-019", "Christopher Rodriguez", "christopher.r@email.com",
            Guid.Parse("01962f9a-f7f8-7a7b-9c8d-0e1f23456789")),
        ("user-020", "Jessica Lewis", "jessica.l@email.com", Guid.Parse("01962f9a-f7f8-7a7b-9c8d-0e1f23456789")),

        // Scotland users
        ("user-021", "Matthew Lee", "matthew.l@email.com", Guid.Parse("01962f9a-f7f8-7b8c-0d9e-1f2345678901")),
        ("user-022", "Karen Walker", "karen.w@email.com", Guid.Parse("01962f9a-f7f8-7b8c-0d9e-1f2345678901")),

        // Wales users
        ("user-023", "Joshua Hall", "joshua.h@email.com", Guid.Parse("01962f9a-f7f8-7c9d-1e0f-23456789012a")),
        ("user-024", "Nancy Allen", "nancy.a@email.com", Guid.Parse("01962f9a-f7f8-7c9d-1e0f-23456789012a")),

        // New South Wales users
        ("user-025", "Andrew Young", "andrew.y@email.com", Guid.Parse("01962f9a-f7f8-7d0e-2f1a-3456789012bc")),
        ("user-026", "Betty Hernandez", "betty.h@email.com", Guid.Parse("01962f9a-f7f8-7d0e-2f1a-3456789012bc")),

        // Victoria users
        ("user-027", "Ryan King", "ryan.k@email.com", Guid.Parse("01962f9a-f7f8-7e1f-3a2b-456789012cde")),
        ("user-028", "Helen Wright", "helen.w@email.com", Guid.Parse("01962f9a-f7f8-7e1f-3a2b-456789012cde")),

        // Queensland users
        ("user-029", "Kevin Lopez", "kevin.l@email.com", Guid.Parse("01962f9a-f7f8-7f2a-4b3c-56789012def0")),
        ("user-030", "Dorothy Hill", "dorothy.h@email.com", Guid.Parse("01962f9a-f7f8-7f2a-4b3c-56789012def0")),

        // Bavaria users
        ("user-031", "Brian Scott", "brian.s@email.com", Guid.Parse("01962f9a-f7f8-7a3b-5c4d-6789012ef012")),
        ("user-032", "Sandra Green", "sandra.g@email.com", Guid.Parse("01962f9a-f7f8-7a3b-5c4d-6789012ef012")),

        // North Rhine-Westphalia users
        ("user-033", "George Adams", "george.a@email.com", Guid.Parse("01962f9a-f7f8-7b4c-6d5e-789012f01234")),
        ("user-034", "Ashley Baker", "ashley.b@email.com", Guid.Parse("01962f9a-f7f8-7b4c-6d5e-789012f01234")),

        // Baden-Württemberg users
        ("user-035", "Kenneth Gonzalez", "kenneth.g@email.com", Guid.Parse("01962f9a-f7f8-7c5d-7e6f-89012f012345")),
        ("user-036", "Kimberly Nelson", "kimberly.n@email.com", Guid.Parse("01962f9a-f7f8-7c5d-7e6f-89012f012345")),

        // Île-de-France users
        ("user-037", "Steven Carter", "steven.c@email.com", Guid.Parse("01962f9a-f7f8-7d6e-8f7a-9012f0123456")),
        ("user-038", "Donna Mitchell", "donna.m@email.com", Guid.Parse("01962f9a-f7f8-7d6e-8f7a-9012f0123456")),

        // Provence-Alpes-Côte d'Azur users
        ("user-039", "Edward Perez", "edward.p@email.com", Guid.Parse("01962f9a-f7f8-7e7f-9a8b-012f01234567")),
        ("user-040", "Carol Roberts", "carol.r@email.com", Guid.Parse("01962f9a-f7f8-7e7f-9a8b-012f01234567")),

        // Nouvelle-Aquitaine users
        ("user-041", "Jason Turner", "jason.t@email.com", Guid.Parse("01962f9a-f7f8-7f8a-0b9c-12f012345678")),
        ("user-042", "Michelle Phillips", "michelle.p@email.com", Guid.Parse("01962f9a-f7f8-7f8a-0b9c-12f012345678")),

        // Tokyo users
        ("user-043", "Jeffrey Campbell", "jeffrey.c@email.com", Guid.Parse("01962f9a-f7f8-7a9b-1c0d-2f01234567ab")),
        ("user-044", "Sarah Parker", "sarah.p@email.com", Guid.Parse("01962f9a-f7f8-7a9b-1c0d-2f01234567ab")),

        // Osaka users
        ("user-045", "Timothy Evans", "timothy.e@email.com", Guid.Parse("01962f9a-f7f8-7b0c-2d1e-f01234567abc")),
        ("user-046", "Laura Edwards", "laura.e@email.com", Guid.Parse("01962f9a-f7f8-7b0c-2d1e-f01234567abc")),

        // Hokkaido users
        ("user-047", "Gary Collins", "gary.c@email.com", Guid.Parse("01962f9a-f7f8-7c1d-3e2f-01234567abcd")),
        ("user-048", "Rebecca Stewart", "rebecca.s@email.com", Guid.Parse("01962f9a-f7f8-7c1d-3e2f-01234567abcd")),

        // Ho Chi Minh City users
        ("user-049", "Jeffrey Sanchez", "jeffrey.s@email.com", Guid.Parse("01962f9a-f7f8-7d2e-4f3a-1234567abcde")),
        ("user-050", "Deborah Morris", "deborah.m@email.com", Guid.Parse("01962f9a-f7f8-7d2e-4f3a-1234567abcde")),

        // Hanoi users
        ("user-051", "Eric Rogers", "eric.r@email.com", Guid.Parse("01962f9a-f7f8-7e3f-5a4b-234567abcdef")),
        ("user-052", "Stephanie Reed", "stephanie.r@email.com", Guid.Parse("01962f9a-f7f8-7e3f-5a4b-234567abcdef")),

        // Da Nang users
        ("user-053", "Stephen Cook", "stephen.c@email.com", Guid.Parse("01962f9a-f7f8-7f4a-6b5c-34567abcdef0")),
        ("user-054", "Sharon Morgan", "sharon.m@email.com", Guid.Parse("01962f9a-f7f8-7f4a-6b5c-34567abcdef0"))
    ];

    public static async Task SeedAsync(Service2Context context, CancellationToken cancellationToken = default)
    {
        var userSet = context.Set<User>();

        foreach (var (userId, name, email, provinceId) in UserData)
        {
            var existedUser = await userSet
                .FirstOrDefaultAsync(a => a.Id == userId, cancellationToken);

            if (existedUser == null)
            {
                userSet.Add(new User
                {
                    Id = userId,
                    Name = name,
                    Email = email,
                    ProvinceId = provinceId.ToString()
                });
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
