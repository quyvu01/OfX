using Microsoft.Extensions.DependencyInjection;

namespace OfX.Abstractions;

public interface IExtensionHandlersInstaller
{
    IServiceCollection ServiceCollection { get; }
}