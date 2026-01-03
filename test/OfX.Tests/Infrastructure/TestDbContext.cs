using Microsoft.EntityFrameworkCore;
using OfX.Tests.TestData.Models;

namespace OfX.Tests.Infrastructure;

public class TestDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Province> Provinces { get; set; } = null!;
    public DbSet<Country> Countries { get; set; } = null!;
    public DbSet<City> Cities { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure relationships
        modelBuilder.Entity<Province>()
            .HasOne(p => p.Country)
            .WithMany(c => c.Provinces)
            .HasForeignKey(p => p.CountryId);

        modelBuilder.Entity<City>()
            .HasOne<Province>()
            .WithMany(p => p.Cities)
            .HasForeignKey(c => c.ProvinceId);
    }
}
