using OfX.Tests.Models;

namespace OfX.Tests.StaticData;

public static class StaticDataTest
{
    public static readonly List<User> Users = [
        new() { Id = "1", Name = "user1", Email = "user1@gm.com" },
        new() { Id = "2", Name = "user2", Email = "user2@gm.com" },
        new() { Id = "3", Name = "user3", Email = "user3@gm.com" }
    ];
}