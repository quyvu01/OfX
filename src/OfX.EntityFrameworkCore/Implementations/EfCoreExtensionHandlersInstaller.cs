using Microsoft.Extensions.DependencyInjection;
using OfX.Abstractions;

namespace OfX.EntityFrameworkCore.Implementations;

public sealed class EfCoreExtensionHandlersInstaller(IServiceCollection serviceCollection) : IExtensionHandlersInstaller
{
    public IServiceCollection ServiceCollection { get; } = serviceCollection;
}