using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OfX.Abstractions;
using OfX.EntityFrameworkCore.Extensions;
using OfX.Extensions;
using OfX.Grpc.Extensions;
using OfX.Tests.Contexts;
using OfX.Tests.Models;
using Xunit;

namespace OfX.Tests;

public class OfXEntityFrameworkTest : ServicesBuilding
{
    public OfXEntityFrameworkTest()
    {
        var assembly = typeof(ITestAssemblyMarker).Assembly;
        InstallService((serviceCollection, _) => serviceCollection
                .AddDbContext<TestDbContext>(opts => opts
                    .UseInMemoryDatabase($"Test_{Guid.NewGuid()}")))
            .InstallService((serviceCollection, _) =>
            {
                serviceCollection.AddOfX(options =>
                    {
                        options.AddContractsContainNamespaces(assembly);
                        options.AddHandlersFromNamespaceContaining<ITestAssemblyMarker>();
                        options.AddGrpcClients(c =>
                        {
                            c.RegisterContractsFromNamespaceContainning<ITestAssemblyMarker>("localhost:5001");
                        });
                    })
                    .AddOfXEFCore<TestDbContext>();
            })
            .InstallAllServices();
        var dbContext = ServiceProvider.GetRequiredService<TestDbContext>();
        dbContext.Users.AddRange(StaticData.StaticDataTest.Users);
        dbContext.SaveChanges();
    }

    [Theory]
    [InlineData("1")]
    public async Task Member_Should_Have_The_Correct_Email(string userId)
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