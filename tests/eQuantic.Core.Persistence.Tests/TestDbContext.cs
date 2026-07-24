using eQuantic.Core.Persistence.Tests.TestEntities;
using Microsoft.EntityFrameworkCore;

namespace eQuantic.Core.Persistence.Tests;

public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
    {
    }

    public DbSet<TestEntity> TestEntities { get; set; }
    public DbSet<TestGenericEntity> TestGenericEntities { get; set; }
    public DbSet<TestUser> TestUsers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}