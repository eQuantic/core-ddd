using eQuantic.Core.Persistence.PostgreSql.Tests.TestEntities;
using Microsoft.EntityFrameworkCore;

namespace eQuantic.Core.Persistence.PostgreSql.Tests;

public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
    {
    }

    public DbSet<ExampleEntity> ExampleEntities { get; set; }
    public DbSet<TestUser> TestUsers { get; set; }
    public DbSet<ProductEntity> ProductEntities { get; set; }
    public DbSet<SimpleEntity> SimpleEntities { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}