using Microsoft.EntityFrameworkCore;
using Service3Api.Models;

namespace Service3Api.Contexts;

public class Service3Context(DbContextOptions<Service3Context> options) : DbContext(options)
{
    public DbSet<Province> Provinces { get; set; }
    public DbSet<Country> Countries { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var provinceEntity = modelBuilder.Entity<Province>();
        provinceEntity.HasKey(a => a.Id);

        provinceEntity.HasOne(a => a.Country)
            .WithMany(a => a.Provinces)
            .HasForeignKey(a => a.CountryId);

        var countryEntity = modelBuilder.Entity<Country>();
        countryEntity.HasKey(a => a.Id);

        base.OnModelCreating(modelBuilder);
    }
}