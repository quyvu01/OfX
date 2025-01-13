using Microsoft.EntityFrameworkCore;
using Service3Api.ModelIds;
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
        provinceEntity.Property(x => x.Id)
            .HasConversion(x => x.Value, id => new ProvinceId(id));
        
        provinceEntity.HasOne(a => a.Country)
            .WithMany(a => a.Provinces)
            .HasForeignKey(a => a.CountryId);

        var countryEntity = modelBuilder.Entity<Country>();
        countryEntity.HasKey(a => a.Id);
        countryEntity.Property(x => x.Id)
            .HasConversion(x => x.Value, id => new CountryId(id));

        base.OnModelCreating(modelBuilder);
    }
}