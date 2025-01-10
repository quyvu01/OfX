using Microsoft.EntityFrameworkCore;
using WorkerService1.ModelIds;
using WorkerService1.Models;

namespace WorkerService1.Contexts;

public class Service2Context(DbContextOptions<Service2Context> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var userEntity = modelBuilder.Entity<User>();
        userEntity.Property(x => x.Id)
            .HasConversion(x => x.Value, id => new UserId(id));
        userEntity.HasKey(a => a.Id);
        base.OnModelCreating(modelBuilder);
    }
}