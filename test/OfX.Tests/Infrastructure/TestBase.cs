using Microsoft.Extensions.DependencyInjection;

namespace OfX.Tests.Infrastructure;

/// <summary>
/// Base class for all OfX tests providing common setup and utilities
/// </summary>
public abstract class TestBase : IDisposable
{
    protected IServiceProvider ServiceProvider { get; private set; }
    protected IServiceCollection Services { get; private set; }

    protected TestBase()
    {
        Services = new ServiceCollection();
        ConfigureServices(Services);
        ServiceProvider = Services.BuildServiceProvider();
    }

    /// <summary>
    /// Override to configure services for specific test classes
    /// </summary>
    protected virtual void ConfigureServices(IServiceCollection services)
    {
        // Default configuration - override in derived classes
    }

    /// <summary>
    /// Get service from DI container
    /// </summary>
    protected T GetService<T>() where T : notnull
    {
        return ServiceProvider.GetRequiredService<T>();
    }

    /// <summary>
    /// Get service or null if not registered
    /// </summary>
    protected T GetServiceOrNull<T>() where T : class
    {
        return ServiceProvider.GetService<T>();
    }

    public virtual void Dispose()
    {
        if (ServiceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
        GC.SuppressFinalize(this);
    }
}
