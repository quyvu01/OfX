using Microsoft.EntityFrameworkCore;
using Service1.Models;

namespace Service1.Contexts;

public sealed class OtherService1Context(DbContextOptions<OtherService1Context> options) : DbContext(options)
{
    public DbSet<MemberAddress> MemberAddresses { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var userEntity = modelBuilder.Entity<MemberAddress>();
        userEntity.HasKey(a => a.Id);
        base.OnModelCreating(modelBuilder);
    }
}