using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using Service1.Contexts;
using Service1.Models;

namespace Service1.Data;

/// <summary>
/// Provides seed data for Service1 (MemberAddress, MemberAdditionalData, MemberSocial).
/// MemberAddress is linked to Provinces from Service3 via ProvinceId FK.
/// </summary>
public static class Service1DataSeeder
{
    #region MemberAddress Data

    private static readonly List<(string Id, Guid ProvinceId, string Street, string City, string ZipCode)>
        MemberAddressData =
        [
            ("addr-001", Guid.Parse("01962f9a-f7f8-7f61-941c-6a086fe96cd2"), "123 Silicon Valley Blvd", "San Francisco",
                "94102"),

            ("addr-002", Guid.Parse("01962f9a-f7f8-7f61-941c-6a086fe96cd2"), "456 Hollywood Dr", "Los Angeles",
                "90028"),

            ("addr-003", Guid.Parse("01962f9a-f7f8-7f61-941c-6a086fe96cd2"), "789 Beach Ave", "San Diego", "92101"),
            ("addr-004", Guid.Parse("01962f9a-f7f8-7f61-941c-6a086fe96cd2"), "321 Tech Park Way", "Palo Alto", "94301"),

            // New York addresses
            ("addr-005", Guid.Parse("01962f9a-f7f8-7b4c-9b4d-eae8ea6e5fc7"), "100 Wall Street", "New York", "10005"),
            ("addr-006", Guid.Parse("01962f9a-f7f8-7b4c-9b4d-eae8ea6e5fc7"), "200 Broadway", "New York", "10007"),
            ("addr-007", Guid.Parse("01962f9a-f7f8-7b4c-9b4d-eae8ea6e5fc7"), "300 Park Ave", "New York", "10022"),
            ("addr-008", Guid.Parse("01962f9a-f7f8-7b4c-9b4d-eae8ea6e5fc7"), "400 Madison Ave", "New York", "10017"),

            // Texas addresses
            ("addr-009", Guid.Parse("01962f9a-f7f8-7e54-a79d-575a8e882eb8"), "500 Main St", "Houston", "77002"),
            ("addr-010", Guid.Parse("01962f9a-f7f8-7e54-a79d-575a8e882eb8"), "600 Congress Ave", "Austin", "78701"),
            ("addr-011", Guid.Parse("01962f9a-f7f8-7e54-a79d-575a8e882eb8"), "700 Dallas Parkway", "Dallas", "75201"),

            // Florida addresses
            ("addr-012", Guid.Parse("01962f9a-f7f8-7a2c-8b3d-4c5e6f789012"), "800 Ocean Drive", "Miami", "33139"),
            ("addr-013", Guid.Parse("01962f9a-f7f8-7a2c-8b3d-4c5e6f789012"), "900 Orange Ave", "Orlando", "32801"),

            // Illinois addresses
            ("addr-014", Guid.Parse("01962f9a-f7f8-7d1b-9c2a-5d6e7f890123"), "1000 Michigan Ave", "Chicago", "60601"),
            ("addr-015", Guid.Parse("01962f9a-f7f8-7d1b-9c2a-5d6e7f890123"), "1100 State St", "Chicago", "60605"),

            // Ontario addresses
            ("addr-016", Guid.Parse("01962f9a-f7f8-7c3d-8e4f-6a7b8c9d0123"), "1200 Bay St", "Toronto", "M5J 2R8"),
            ("addr-017", Guid.Parse("01962f9a-f7f8-7c3d-8e4f-6a7b8c9d0123"), "1300 Yonge St", "Toronto", "M4T 1W6"),

            // Quebec addresses
            ("addr-018", Guid.Parse("01962f9a-f7f8-7e5f-9a6b-7c8d9e0f1234"), "1400 Rue Saint-Denis", "Montreal",
                "H2X 3K2"),

            ("addr-019", Guid.Parse("01962f9a-f7f8-7e5f-9a6b-7c8d9e0f1234"), "1500 Grande Allée", "Quebec City",
                "G1R 2J5"),

            // British Columbia addresses

            ("addr-020", Guid.Parse("01962f9a-f7f8-7f6a-8b7c-9d0e1f234567"), "1600 Robson St", "Vancouver", "V6G 1C7"),
            ("addr-021", Guid.Parse("01962f9a-f7f8-7f6a-8b7c-9d0e1f234567"), "1700 Government St", "Victoria",
                "V8W 1Y4"),

            // England addresses

            ("addr-022", Guid.Parse("01962f9a-f7f8-7a7b-9c8d-0e1f23456789"), "1800 Oxford St", "London", "W1D 1BS"),
            ("addr-023", Guid.Parse("01962f9a-f7f8-7a7b-9c8d-0e1f23456789"), "1900 Baker St", "London", "NW1 6XE"),

            // Scotland addresses
            ("addr-024", Guid.Parse("01962f9a-f7f8-7b8c-0d9e-1f2345678901"), "2000 Royal Mile", "Edinburgh", "EH1 1RE"),
            ("addr-025", Guid.Parse("01962f9a-f7f8-7b8c-0d9e-1f2345678901"), "2100 Buchanan St", "Glasgow", "G1 3HL"),

            // Wales addresses
            ("addr-026", Guid.Parse("01962f9a-f7f8-7c9d-1e0f-23456789012a"), "2200 Queen St", "Cardiff", "CF10 2BH"),

            // Australia addresses
            ("addr-027", Guid.Parse("01962f9a-f7f8-7d0e-2f1a-3456789012bc"), "2300 George St", "Sydney", "2000"),
            ("addr-028", Guid.Parse("01962f9a-f7f8-7e1f-3a2b-456789012cde"), "2400 Collins St", "Melbourne", "3000"),
            ("addr-029", Guid.Parse("01962f9a-f7f8-7f2a-4b3c-56789012def0"), "2500 Queen St", "Brisbane", "4000"),

            // Germany addresses
            ("addr-030", Guid.Parse("01962f9a-f7f8-7a3b-5c4d-6789012ef012"), "2600 Marienplatz", "Munich", "80331"),

            // France addresses
            ("addr-031", Guid.Parse("01962f9a-f7f8-7d6e-8f7a-9012f0123456"), "2700 Champs-Élysées", "Paris", "75008"),

            // Japan addresses
            ("addr-032", Guid.Parse("01962f9a-f7f8-7a9b-1c0d-2f01234567ab"), "2800 Shibuya", "Tokyo", "150-0002"),
            ("addr-033", Guid.Parse("01962f9a-f7f8-7b0c-2d1e-f01234567abc"), "2900 Dotonbori", "Osaka", "542-0071"),

            // Vietnam addresses
            ("addr-034", Guid.Parse("01962f9a-f7f8-7d2e-4f3a-1234567abcde"), "3000 Nguyen Hue", "Ho Chi Minh City",
                "700000"),

            ("addr-035", Guid.Parse("01962f9a-f7f8-7e3f-5a4b-234567abcdef"), "3100 Hoan Kiem", "Hanoi", "100000")
        ];

    #endregion

    #region MemberAdditionalData

    private static readonly List<(string Id, string Name, string Bio, DateTime JoinDate)> MemberAdditionalDataData =
    [
        ("member-001", "Alice Cooper", "Software Engineer specializing in distributed systems",
            new DateTime(2020, 1, 15)),

        ("member-002", "Bob Dylan", "Product Manager with 10 years experience", new DateTime(2019, 3, 22)),
        ("member-003", "Charlie Chaplin", "UX Designer passionate about user experience",
            new DateTime(2021, 5, 10)),

        ("member-004", "Diana Prince", "DevOps Engineer | Cloud Infrastructure Expert", new DateTime(2018, 7, 8)),
        ("member-005", "Edward Norton", "Full Stack Developer | Open Source Contributor",
            new DateTime(2020, 9, 14)),

        ("member-006", "Fiona Apple", "Data Scientist | ML Enthusiast", new DateTime(2021, 11, 20)),
        ("member-007", "George Harrison", "Backend Developer | API Architect", new DateTime(2019, 2, 28)),
        ("member-008", "Helen Mirren", "Frontend Developer | React Specialist", new DateTime(2022, 4, 5)),
        ("member-009", "Isaac Newton", "System Architect | Performance Optimization", new DateTime(2017, 6, 12)),
        ("member-010", "Julia Roberts", "Quality Assurance Engineer | Test Automation", new DateTime(2020, 8, 19)),
        ("member-011", "Kevin Bacon", "Security Engineer | Penetration Testing", new DateTime(2021, 10, 25)),
        ("member-012", "Laura Dern", "Database Administrator | SQL Expert", new DateTime(2019, 12, 3)),
        ("member-013", "Michael Jordan", "Mobile Developer | iOS/Android", new DateTime(2020, 2, 11)),
        ("member-014", "Natalie Portman", "Technical Writer | Documentation Specialist", new DateTime(2021, 4, 17)),
        ("member-015", "Oscar Wilde", "AI/ML Engineer | Deep Learning", new DateTime(2022, 6, 23)),
        ("member-016", "Patricia Arquette", "Cloud Architect | AWS/Azure", new DateTime(2018, 8, 29)),
        ("member-017", "Quentin Tarantino", "Site Reliability Engineer | SRE", new DateTime(2019, 10, 6)),
        ("member-018", "Rachel Green", "Scrum Master | Agile Coach", new DateTime(2020, 12, 13)),
        ("member-019", "Samuel Jackson", "Blockchain Developer | Web3", new DateTime(2021, 2, 19)),
        ("member-020", "Tina Turner", "Business Analyst | Requirements Engineering", new DateTime(2022, 4, 26))
    ];

    #endregion

    #region MemberSocial Data

    private static readonly
        List<(int Id, string Name, string Platform, string Handle, List<(string Key, string Value, int Order)> Metadata
            )> MemberSocialData =
        [
            (1, "Tech Influencer Alpha", "Twitter", "@techinfluencer", [
                ("Followers", "150000", 1),
                ("Verified", "True", 2),
                ("Category", "Technology", 3)
            ]),

            (2, "Code Master Beta", "GitHub", "codemaster", [
                ("Stars", "25000", 1),
                ("Repositories", "150", 2),
                ("Language", "C#", 3)
            ]),

            (3, "Design Guru Gamma", "Dribbble", "designguru", [
                ("Likes", "50000", 1),
                ("Projects", "200", 2),
                ("Specialty", "UI/UX", 3)
            ]),

            (4, "DevOps Expert Delta", "LinkedIn", "devopsexpert", [
                ("Connections", "5000", 1),
                ("Posts", "500", 2),
                ("Focus", "Cloud Infrastructure", 3)
            ]),

            (5, "Data Scientist Epsilon", "Kaggle", "datascientist", [
                ("Rank", "Expert", 1),
                ("Competitions", "25", 2),
                ("Gold Medals", "5", 3)
            ]),

            (6, "Security Researcher Zeta", "Twitter", "@securityresearcher", [
                ("CVEs", "12", 1),
                ("Bounties", "50000", 2),
                ("Area", "Web Security", 3)
            ]),

            (7, "Mobile Dev Eta", "Medium", "@mobiledev", [
                ("Articles", "100", 1),
                ("Followers", "25000", 2),
                ("Topics", "iOS,Android", 3)
            ]),

            (8, "Cloud Architect Theta", "YouTube", "cloudarchitect", [
                ("Subscribers", "100000", 1),
                ("Videos", "250", 2),
                ("Focus", "AWS,Azure", 3)
            ]),

            (9, "AI Researcher Iota", "arXiv", "airesearcher", [
                ("Papers", "15", 1),
                ("Citations", "500", 2),
                ("Field", "Deep Learning", 3)
            ]),

            (10, "Blockchain Dev Kappa", "GitHub", "blockchaindev", [
                ("Smart Contracts", "50", 1),
                ("Audits", "20", 2),
                ("Chain", "Ethereum", 3)
            ])
        ];

    #endregion

    public static async Task SeedServiceMemberAddressAsync(OtherService1Context context,
        CancellationToken cancellationToken = default)
    {
        var memberAddressSet = context.Set<MemberAddress>();

        foreach (var (id, provinceId, street, city, zipCode) in MemberAddressData)
        {
            var existedMemberAddress = await memberAddressSet
                .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

            if (existedMemberAddress == null)
            {
                memberAddressSet.Add(new MemberAddress
                {
                    Id = id,
                    ProvinceId = provinceId.ToString(),
                    Street = street,
                    City = city,
                    ZipCode = zipCode
                });
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public static async Task SeedMemberAdditionalDataAsync(Service1Context context,
        CancellationToken cancellationToken = default)
    {
        var memberAdditionalDataSet = context.Set<MemberAdditionalData>();

        foreach (var (id, name, bio, joinDate) in MemberAdditionalDataData)
        {
            var existedMemberAdditionalData = await memberAdditionalDataSet
                .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

            if (existedMemberAdditionalData == null)
            {
                memberAdditionalDataSet.Add(new MemberAdditionalData
                {
                    Id = id,
                    Name = name
                    // Note: Bio, JoinDate properties might not exist in the model
                    // If they do, uncomment below:
                    // Bio = bio,
                    // JoinDate = joinDate
                });
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public static async Task SeedMemberSocialAsync(IMongoCollection<MemberSocial> memberSocialCollection,
        CancellationToken cancellationToken = default)
    {
        foreach (var (id, name, platform, handle, metadata) in MemberSocialData)
        {
            var existed = await memberSocialCollection
                .Find(m => m.Id.Equals(id))
                .FirstOrDefaultAsync(cancellationToken);

            if (existed != null)
                await memberSocialCollection.DeleteOneAsync(x => x.Id == id, cancellationToken: cancellationToken);

            await memberSocialCollection.InsertOneAsync(new MemberSocial
            {
                Id = id,
                Name = name,
                OtherValue = $"{platform}: {handle}",
                CreatedTime = DateTime.UtcNow,
                Metadata = metadata.Select(m => new MemerSocialMetadata
                {
                    Key = m.Key,
                    Value = m.Value,
                    Order = m.Order,
                    ExternalOfMetadata = new ExternalOfMetadata
                    {
                        JustForTest = $"Metadata for {name}"
                    }
                }).ToList()
            }, cancellationToken: cancellationToken);
        }
    }
}