using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Shared.RunSqlMigration;

public static class MigrationDatabase
{
    public static async Task MigrationDatabaseAsync<T>(IHost host) where T : DbContext
    {
        using var serviceScope = host.Services.CreateScope();
        var dbContext = serviceScope.ServiceProvider.GetRequiredService<T>();
        try
        {
            var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
            if (!pendingMigrations.Any()) return;
            await dbContext.Database.MigrateAsync();
        }
        catch (Exception e)
        {
            // ignored
        }
    }
}