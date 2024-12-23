using Microsoft.Extensions.DependencyInjection;
using OfX.Abstractions;
using OfX.Extensions;
using OfX.Tests.Models;
using Xunit;

namespace OfX.Tests;

public class AttributeDataTests : ServicesBuilding
{
    public AttributeDataTests() => InstallServices();

    private void InstallServices()
    {
        var assembly = typeof(AttributeDataTests).Assembly;
        InstallService((serviceCollection, _) => serviceCollection.AddOfX(r =>
            {
                r.MapForContractsContainsAssemblies([assembly]);
                r.HandlerContainsAssemblies([assembly]);
            }))
            .InstallAllServices();
    }

    [Theory]
    [InlineData("1")]
    [InlineData("2")]
    [InlineData("3")]
    public async Task Member_Must_Be_Matches_With_User(string userId)
    {
        var member = new Member { UserId = userId };
        var userName = StaticData.StaticDataTest.Users.FirstOrDefault(a => a.Id == userId)?.Name;
        var dataMappableService = ServiceProvider.GetRequiredService<IDataMappableService>();
        await dataMappableService.MapDataAsync(member, CancellationToken.None);
        Assert.Equal(userName, member.UserName);
    }

    [Theory]
    [InlineData("4")]
    public async Task Member_Must_Not_Match_With_User_When_Id_Not_Found(string userId)
    {
        var member = new Member { UserId = userId };
        var dataMappableService = ServiceProvider.GetRequiredService<IDataMappableService>();
        await dataMappableService.MapDataAsync(member, CancellationToken.None);
        Assert.Null(member.UserName);
    }
}