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

                options.AddStronglyTypeIdConverter(c => c.OfType<IdTestRegister>());
            }))
            .InstallAllServices();
    }

    [Theory]
    [InlineData("3306d98a-4a74-4965-89d5-8a64eb49635f")]
    public void Guid_Id_Should_Be_Converted(string id)
    {
        var userIdStronglyTypeService = ServiceProvider.GetService<IStronglyTypeConverter<UserId>>();
        Assert.NotNull(userIdStronglyTypeService);
        var canConvert = userIdStronglyTypeService.CanConvert(id);
        Assert.True(canConvert);
    }

    [Theory]
    [InlineData("123123")]
    public void Long_Id_Should_Be_Converted(string id)
    {
        var longIdStronglyTypeService = ServiceProvider.GetService<IStronglyTypeConverter<LongTestId>>();
        Assert.NotNull(longIdStronglyTypeService);
        var canConvert = longIdStronglyTypeService.CanConvert(id);
        Assert.True(canConvert);
    }
}

internal sealed class IdTestRegister :
    IStronglyTypeConverter<LongTestId>,
    IStronglyTypeConverter<UserId>,
    IStronglyTypeConverter<ProvinceId>

{
    public LongTestId Convert(string input) => new(long.Parse(input));

    public bool CanConvert(string input) => long.TryParse(input, out _);
    UserId IStronglyTypeConverter<UserId>.Convert(string input) => new(Guid.Parse(input));

    bool IStronglyTypeConverter<UserId>.CanConvert(string input) => Guid.TryParse(input, out _);

    ProvinceId IStronglyTypeConverter<ProvinceId>.Convert(string input) => new(Guid.Parse(input));

    bool IStronglyTypeConverter<ProvinceId>.CanConvert(string input) => Guid.TryParse(input, out _);
}

internal sealed record LongTestId(long Value) : StronglyTypedId<long>(Value)
{
    public override string ToString() => base.ToString();
}