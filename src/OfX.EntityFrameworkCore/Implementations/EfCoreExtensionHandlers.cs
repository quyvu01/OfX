using Microsoft.Extensions.DependencyInjection;
using OfX.Abstractions;

namespace OfX.EntityFrameworkCore.Implementations;

public sealed class EfCoreExtensionHandlers(IServiceCollection serviceCollection) : IExtensionHandlers
{
    public IServiceCollection ServiceCollection { get; } = serviceCollection;
}