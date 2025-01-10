using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OfX.Extensions;
using OfX.Grpc.Exceptions;
using OfX.Tests.Contexts;
using OfX.Tests.StronglyTypes;
using Xunit;

namespace OfX.Tests;

public sealed class OfXStronglyTypeTests : ServicesBuilding
{
    [Fact]
    public void Should_Have_Strongly_Type_Configuration()
    {
        try
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
        catch (Exception e)
        {
            Assert.Equal(typeof(OfXGrpcExceptions.GrpcHostHasBeenRegistered), e.GetType());
        }
    }
}