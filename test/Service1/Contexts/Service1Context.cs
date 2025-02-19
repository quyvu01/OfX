using Microsoft.EntityFrameworkCore;
using Service1.Models;

namespace Service1.Contexts;

public sealed class Service1Context(DbContextOptions<Service1Context> options) : DbContext(options)
{
    public DbSet<MemberAdditionalData> MemberAdditionalDatas { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var userEntity = modelBuilder.Entity<MemberAdditionalData>();
        userEntity.HasKey(a => a.Id);
        base.OnModelCreating(modelBuilder);
    }
}