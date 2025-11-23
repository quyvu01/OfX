using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OfX.Abstractions;
using OfX.EntityFrameworkCore.Extensions;
using OfX.Extensions;
using OfX.Tests.Contexts;
using OfX.Tests.Models;
using Xunit;

namespace OfX.Tests;

public class OfXEfCoreTests : ServicesBuilding
{
    public OfXEfCoreTests()
    {
        var assembly = typeof(ITestAssemblyMarker).Assembly;
        InstallService((serviceCollection, _) => serviceCollection
                .AddDbContext<TestDbContext>(opts => opts
                    .UseInMemoryDatabase($"Test_{Guid.NewGuid()}")))
            .InstallService((serviceCollection, _) => serviceCollection.AddOfX(options =>
                {
                    options.AddAttributesContainNamespaces(assembly);
                    options.AddModelConfigurationsFromNamespaceContaining<ITestAssemblyMarker>();
                })
                .AddOfXEFCore(options => options.AddDbContexts(typeof(TestDbContext))))
            .InstallAllServices();
        var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();
        dbContext.Users.AddRange(StaticData.StaticDataTest.Users);
        dbContext.SaveChanges();
    }

    [Theory]
    [InlineData("1")]
    public async Task Member_Should_Have_The_Correct_User_Data(string userId)
    {
        var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();
        var member = new Member { UserId = userId };
        var user = await dbContext.Users.FirstOrDefaultAsync(a => a.Id == userId);
        var dataMappableService = ServiceProvider.GetRequiredService<IDataMappableService>();
        await dataMappableService.MapDataAsync(member);
        Assert.Equal(user?.Name, member.UserName);
        Assert.Equal(user?.Email, member.UserEmail);
    }
}