using Microsoft.Extensions.DependencyInjection;

namespace OfX.Abstractions;

public interface IExtensionHandlers
{
    IServiceCollection ServiceCollection { get; }
}