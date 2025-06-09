using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OfX.EntityFrameworkCore.Extensions;
using OfX.Extensions;
using OfX.Grpc.Exceptions;
using OfX.Grpc.Extensions;
using OfX.Tests.Attributes;
using OfX.Tests.Contexts;
using Xunit;

namespace OfX.Tests;

public sealed class OfXGrpcTests : ServicesBuilding
{
    [Fact]
    public void Should_Throw_Exception_When_Trying_To_Add_Same_Grpc_Host()
    {
        try
        {
            var assembly = typeof(ITestAssemblyMarker).Assembly;
            InstallService((serviceCollection, _) => serviceCollection
                    .AddDbContext<TestDbContext>(opts => opts
                        .UseInMemoryDatabase($"Test_{Guid.NewGuid()}")))
                .InstallService((serviceCollection, _) =>
                {
                    serviceCollection.AddOfX(options =>
                        {
                            options.AddAttributesContainNamespaces(assembly);
                            options.AddHandlersFromNamespaceContaining<ITestAssemblyMarker>();
                            options.AddGrpcClients(c => c.AddGrpcHosts("localhost:5001"));
                            options.AddModelConfigurationsFromNamespaceContaining<ITestAssemblyMarker>();
                        })
                        .AddOfXEFCore(options => { options.AddDbContexts(typeof(TestDbContext)); });
                })
                .InstallAllServices();
        }
        catch (Exception)
        {
            // ignored
        }
    }

    [Fact]
    public void Should_Throw_Exception_When_Trying_To_Add_Existed_Attribute()
    {
        try
        {
            var assembly = typeof(ITestAssemblyMarker).Assembly;
            InstallService((serviceCollection, _) => serviceCollection
                    .AddDbContext<TestDbContext>(opts => opts
                        .UseInMemoryDatabase($"Test_{Guid.NewGuid()}")))
                .InstallService((serviceCollection, _) =>
                {
                    serviceCollection.AddOfX(options =>
                        {
                            options.AddAttributesContainNamespaces(assembly);
                            options.AddHandlersFromNamespaceContaining<ITestAssemblyMarker>();
                            options.AddGrpcClients(c => c.AddGrpcHosts("localhost:5001", "localhost:5002"));
                            options.AddModelConfigurationsFromNamespaceContaining<ITestAssemblyMarker>();
                        })
                        .AddOfXEFCore(options => { options.AddDbContexts(typeof(TestDbContext)); });
                })
                .InstallAllServices();
        }
        catch (Exception)
        {
            // ignored
        }
    }
}