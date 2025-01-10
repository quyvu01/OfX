using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OfX.Abstractions;
using OfX.Extensions;
using OfX.Tests.Contexts;
using OfX.Tests.StronglyTypes;
using Xunit;

namespace OfX.Tests;

public sealed class OfXStronglyTypeTests : ServicesBuilding
{
    public OfXStronglyTypeTests()
    {
        var assembly = typeof(ITestAssemblyMarker).Assembly;
        InstallService((serviceCollection, _) => serviceCollection
                .AddDbContext<TestDbContext>(opts => opts
                    .UseInMemoryDatabase($"Test_{Guid.NewGuid()}")))
            .InstallService((serviceCollection, _) => serviceCollection.AddOfX(options =>
            {
                options.AddAttributesContainNamespaces(assembly);
                options.AddHandlersFromNamespaceContaining<ITestAssemblyMarker>();
                options.AddStronglyTypeIdConverter(c => c.ForType<StronglyTypeTestsRegister>());
            }))
            .InstallAllServices();
    }

    [Theory]
    [InlineData("3306d98a-4a74-4965-89d5-8a64eb49635f")]
    public void Id_Should_Be_Converted(string id)
    {
        var userIdStronglyTypeService = ServiceProvider.GetService<IStronglyTypeConverter<UserId>>();
        Assert.NotNull(userIdStronglyTypeService);
        var canConvert = userIdStronglyTypeService.CanConvert(id);
        Assert.True(canConvert);
    }
}