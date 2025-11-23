using Microsoft.EntityFrameworkCore;
using OfX.Tests.Models;

namespace OfX.Tests.Contexts;

public class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var userEntity = modelBuilder.Entity<User>();
        userEntity.HasKey(a => a.Id);
        base.OnModelCreating(modelBuilder);
    }
}