using OfX.Tests.Infrastructure.Builders;
using OfX.Tests.TestData.Models;

namespace OfX.Tests.TestData.Builders;

public class UserBuilder : TestEntityBuilder<User, UserBuilder>
{
    private static int _idCounter = 1;

    protected override void SetDefaults()
    {
        var id = _idCounter++;
        Entity.Id = $"user-{id}";
        Entity.Name = $"User {id}";
        Entity.Email = $"user{id}@test.com";
        Entity.ProvinceId = "province-1";
        Entity.CreatedAt = DateTime.UtcNow;
        Entity.IsActive = true;
    }

    public UserBuilder WithId(string id)
    {
        Entity.Id = id;
        return This();
    }

    public UserBuilder WithName(string name)
    {
        Entity.Name = name;
        return This();
    }

    public UserBuilder WithEmail(string email)
    {
        Entity.Email = email;
        return This();
    }

    public UserBuilder WithProvinceId(string provinceId)
    {
        Entity.ProvinceId = provinceId;
        return This();
    }

    public UserBuilder IsInactive()
    {
        Entity.IsActive = false;
        return This();
    }

    public UserBuilder CreatedAt(DateTime date)
    {
        Entity.CreatedAt = date;
        return This();
    }

    /// <summary>
    /// Create a user with all required relationships for testing
    /// </summary>
    public static User JohnDoe() => new UserBuilder()
        .WithId("john-doe")
        .WithName("John Doe")
        .WithEmail("john.doe@example.com")
        .WithProvinceId("california")
        .Build();

    public static User JaneSmith() => new UserBuilder()
        .WithId("jane-smith")
        .WithName("Jane Smith")
        .WithEmail("jane.smith@example.com")
        .WithProvinceId("texas")
        .Build();
}
