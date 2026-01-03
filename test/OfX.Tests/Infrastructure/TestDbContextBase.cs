using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace OfX.Tests.Infrastructure;

/// <summary>
/// Base class for tests requiring database context
/// </summary>
/// <typeparam name="TContext">The DbContext type</typeparam>
public abstract class TestDbContextBase<TContext> : TestBase where TContext : DbContext
{
    protected TContext DbContext { get; private set; }

    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);

        // Use in-memory database with unique name per test
        var dbName = $"Test_{typeof(TContext).Name}_{Guid.NewGuid()}";

        services.AddDbContext<TContext>(options =>
            options.UseInMemoryDatabase(dbName));
    }

    protected TestDbContextBase()
    {
        DbContext = GetService<TContext>();
        SeedDatabase();
    }

    /// <summary>
    /// Override to seed test data
    /// </summary>
    protected virtual void SeedDatabase()
    {
        // Default: no seeding
    }

    /// <summary>
    /// Clear and reseed database
    /// </summary>
    protected void ResetDatabase()
    {
        DbContext.Database.EnsureDeleted();
        DbContext.Database.EnsureCreated();
        SeedDatabase();
    }

    public override void Dispose()
    {
        DbContext?.Dispose();
        base.Dispose();
    }
}
