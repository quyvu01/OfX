using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using OfX.Constants;
using OfX.Statics;

namespace OfX.Registries;

public class OfXRegister(IServiceCollection serviceCollection)
{
    public IServiceCollection ServiceCollection { get; } = serviceCollection;

    public void AddHandlersFromNamespaceContaining<TAssemblyMarker>() =>
        OfXStatics.HandlersRegister = typeof(TAssemblyMarker).Assembly;

    public void AddAttributesContainNamespaces(params Assembly[] attributeAssemblies) =>
        OfXStatics.AttributesRegister = [..attributeAssemblies];

    public void AddModelConfigurationsFromNamespaceContaining<TAssembly>() =>
        OfXStatics.ModelConfigurationAssembly = typeof(TAssembly).Assembly;

    public void ThrowIfException() => OfXStatics.ThrowIfExceptions = true;

    public void SetMaxObjectSpawnTimes(int maxObjectSpawnTimes)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxObjectSpawnTimes);
        OfXStatics.MaxObjectSpawnTimes = maxObjectSpawnTimes;
    }

    public void SetRequestTimeOut(TimeSpan timeout)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(timeout, TimeSpan.Zero);
        OfXConstants.DefaultRequestTimeout = timeout;
    }
}