using Microsoft.EntityFrameworkCore;
using Service2.Models;

namespace Service2.Contexts;

public class Service2Context(DbContextOptions<Service2Context> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var userEntity = modelBuilder.Entity<User>();
        userEntity.HasKey(a => a.Id);
        base.OnModelCreating(modelBuilder);
    }
}