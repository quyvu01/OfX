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

    [Fact]
    public async Task User_Attribute_Should_Be_0()
    {
        var member = new Member { UserId = "123" };
        var dataMappableService = ServiceProvider.GetRequiredService<IDataMappableService>();
        await dataMappableService.MapDataAsync(member, CancellationToken.None);
        Assert.Equal("0", member.UserName);
    }

    [Fact]
    public async Task User_Attribute_Should_Not_Be_1()
    {
        var member = new Member { UserId = "123" };
        var dataMappableService = ServiceProvider.GetRequiredService<IDataMappableService>();
        await dataMappableService.MapDataAsync(member, CancellationToken.None);
        Assert.NotEqual("1", member.UserName);
    }
}