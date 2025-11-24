using Microsoft.Extensions.DependencyInjection;
using OfX.Abstractions;
using OfX.Exceptions;
using OfX.Extensions;
using Shouldly;
using Xunit;

namespace OfX.Tests.MicrosoftExtensionsDI;

public sealed class AssemblyResolutionTests
{
    private readonly IServiceProvider _serviceProvider;

    public AssemblyResolutionTests()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddOfX(c => c.AddAttributesContainNamespaces(typeof(ITestAssemblyMarker).Assembly));
        _serviceProvider = serviceCollection.BuildServiceProvider();
    }

    [Fact]
    public void Should_resolve_data_mappable_service()
    {
        _serviceProvider.GetService<IDataMappableService>().ShouldNotBeNull();
    }

    [Fact]
    public void Should_required_at_least_one_attributes_contain_namespace()
    {
        var serviceCollection = new ServiceCollection();
        Action registration = () => serviceCollection.AddOfX(_ => { });
        registration.ShouldThrow<OfXException.OfXAttributesMustBeSet>();
    }
}