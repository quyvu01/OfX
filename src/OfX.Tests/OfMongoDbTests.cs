using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using OfX.Abstractions;
using OfX.Extensions;
using OfX.MongoDb.Extensions;
using OfX.Nats.Extensions;
using OfX.Tests.Models;
using Xunit;

namespace OfX.Tests;

public class OfMongoDbTests : ServicesBuilding
{
    private readonly string _databaseName;

    public OfMongoDbTests()
    {
        _databaseName = Guid.NewGuid().ToString().Replace("-", "");
        var client = new MongoClient("mongodb://localhost:27017");
        var database = client.GetDatabase(_databaseName);
        var userCollection = database.GetCollection<User>("Users");
        var assembly = typeof(ITestAssemblyMarker).Assembly;
        InstallService((serviceCollection, _) =>
            {
                serviceCollection.AddOfX(options =>
                    {
                        options.AddAttributesContainNamespaces(assembly);
                        options.AddNats(config => config.Url("nats://localhost:4222"));
                        options.AddModelConfigurationsFromNamespaceContaining<ITestAssemblyMarker>();
                    })
                    .AddMongoDb(c => c.AddCollection(userCollection));
                serviceCollection.AddScoped(_ => userCollection);
                serviceCollection.AddScoped<IMongoClient>(_ => client);
            })
            .InstallAllServices();
        var asyncCursor = userCollection
            .Find(x => StaticData.StaticDataTest.Users.Select(u => u.Id).Contains(x.Id));
        var users = asyncCursor.ToList();
        if (users.Count == StaticData.StaticDataTest.Users.Count) return;
        userCollection.InsertMany(StaticData.StaticDataTest.Users);
    }

    [Theory]
    [InlineData("1")]
    public async Task Member_Should_Have_The_Correct_User_Data(string userId)
    {
        var collection = ServiceProvider.GetRequiredService<IMongoCollection<User>>();
        var client = ServiceProvider.GetRequiredService<IMongoClient>();
        var member = new Member { UserId = userId };
        var asyncCursor = await collection.FindAsync(a => a.Id == userId);
        var user = await asyncCursor.FirstAsync();
        var dataMappableService = ServiceProvider.GetRequiredService<IDataMappableService>();
        await dataMappableService.MapDataAsync(member);
        Assert.Equal(user?.Name, member.UserName);
        Assert.Equal(user?.Email, member.UserEmail);
        await client.DropDatabaseAsync(_databaseName);
    }
}