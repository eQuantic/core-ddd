using eQuantic.Core.DataModel;
using eQuantic.Core.Domain.Entities;
using eQuantic.Core.Persistence.PostgreSql.Extensions;
using eQuantic.Core.Persistence.PostgreSql.Options;
using eQuantic.Core.Persistence.PostgreSql.Tests.TestEntities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace eQuantic.Core.Persistence.PostgreSql.Tests.Extensions;

[TestFixture]
public class ModelBuilderExtensionsTests
{
    private DbContextOptions<TestDbContext> _options;

    [SetUp]
    public void SetUp()
    {
        _options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Test]
    public void ApplyPostgreSqlDataModelConventions_WithDefaultOptions_ShouldApplySnakeCaseConventions()
    {
        using var context = new TestDbContext(_options);
        var modelBuilder = new ModelBuilder();
        
        modelBuilder.Entity<ProductEntity>();
        
        modelBuilder.ApplyPostgreSqlDataModelConventions();

        var entityType = modelBuilder.Model.FindEntityType(typeof(ProductEntity));
        entityType.Should().NotBeNull();
        
        var tableName = entityType!.GetTableName();
        tableName.Should().Be("product_entities");
    }

    [Test]
    public void ApplyPostgreSqlDataModelConventions_WithEntityTimeMark_ShouldSetDefaultValueForCreatedAt()
    {
        using var context = new TestDbContext(_options);
        var modelBuilder = new ModelBuilder();
        
        modelBuilder.Entity<ExampleEntity>();
        
        modelBuilder.ApplyPostgreSqlDataModelConventions();

        var entityType = modelBuilder.Model.FindEntityType(typeof(ExampleEntity));
        entityType.Should().NotBeNull();
        
        var createdAtProperty = entityType!.FindProperty(nameof(IEntityTimeMark.CreatedAt));
        createdAtProperty.Should().NotBeNull();
        createdAtProperty!.GetDefaultValueSql().Should().Be("NOW() AT TIME ZONE 'UTC'");
    }

    [Test]
    public void ApplyPostgreSqlDataModelConventions_WithoutEntityTimeMark_ShouldNotSetDefaultValue()
    {
        using var context = new TestDbContext(_options);
        var modelBuilder = new ModelBuilder();
        
        modelBuilder.Entity<SimpleEntity>();
        
        modelBuilder.ApplyPostgreSqlDataModelConventions();

        var entityType = modelBuilder.Model.FindEntityType(typeof(SimpleEntity));
        entityType.Should().NotBeNull();
        
        var properties = entityType!.GetProperties();
        properties.Should().NotContain(p => p.GetDefaultValueSql() == "NOW() AT TIME ZONE 'UTC'");
    }

    [Test]
    public void ApplyPostgreSqlDataModelConventions_WithEntityAuditingDisabled_ShouldNotSetDefaultValue()
    {
        using var context = new TestDbContext(_options);
        var modelBuilder = new ModelBuilder();
        
        modelBuilder.Entity<ExampleEntity>().Property(o => o.CreatedAt);
        
        modelBuilder.ApplyPostgreSqlDataModelConventions(options =>
        {
            options.EnableEntityAuditing(false);
        });

        var entityType = modelBuilder.Model.FindEntityType(typeof(ExampleEntity));
        entityType.Should().NotBeNull();
        
        var createdAtProperty = entityType!.FindProperty(nameof(IEntityTimeMark.CreatedAt));
        createdAtProperty.Should().NotBeNull();
        createdAtProperty!.GetDefaultValueSql().Should().BeNull();
    }

    [Test]
    public void ApplyPostgreSqlDataModelConventions_WithUserRelationships_ShouldConfigureForeignKeysWithSnakeCase()
    {
        using var context = new TestDbContext(_options);
        var modelBuilder = new ModelBuilder();
        
        modelBuilder.Entity<ExampleEntity>().HasKey(o => o.Id);
        modelBuilder.Entity<TestUser>().HasKey(o => o.Id);
        
        modelBuilder.ApplyPostgreSqlDataModelConventions();

        var entityType = modelBuilder.Model.FindEntityType(typeof(ExampleEntity));
        entityType.Should().NotBeNull();
        
        var tableName = entityType!.GetTableName();
        tableName.Should().Be("example_entities");
        
        var foreignKeys = entityType.GetForeignKeys();
        foreignKeys.Should().HaveCount(3);
        
        var createdByForeignKey = foreignKeys.FirstOrDefault(fk => 
            fk.Properties.Any(p => p.Name == nameof(ExampleEntity.CreatedById)));
        createdByForeignKey.Should().NotBeNull();
        createdByForeignKey!.IsRequired.Should().BeTrue();
    }

    [Test]
    public void ApplyPostgreSqlDataModelConventions_WithFullyQualifiedPrimaryKeys_ShouldConfigurePrimaryKeyColumnNames()
    {
        using var context = new TestDbContext(_options);
        var modelBuilder = new ModelBuilder();
        
        modelBuilder.Entity<ProductEntity>().HasKey(o => o.Id);
        
        modelBuilder.ApplyPostgreSqlDataModelConventions(options =>
        {
            options.RemoveEntitySuffixFromTableName("Entity");
            options.UseFullyQualifiedPrimaryKeys();
        });

        var entityType = modelBuilder.Model.FindEntityType(typeof(ProductEntity));
        entityType.Should().NotBeNull();
        
        var tableName = entityType!.GetTableName();
        tableName.Should().Be("products");
        
        var idProperty = entityType.FindProperty(nameof(ProductEntity.Id));
        idProperty.Should().NotBeNull();
        idProperty!.GetColumnName().Should().Be("product_id");
    }

    [Test]
    public void ApplyPostgreSqlDataModelConventions_WithCustomOptions_ShouldApplyCustomConfiguration()
    {
        using var context = new TestDbContext(_options);
        var modelBuilder = new ModelBuilder();
        
        modelBuilder.Entity<ProductEntity>();
        
        modelBuilder.ApplyPostgreSqlDataModelConventions(options =>
        {
            options.RemoveEntitySuffixFromTableName("Entity");
            options.EnableEntityAuditing();
        });

        var entityType = modelBuilder.Model.FindEntityType(typeof(ProductEntity));
        entityType.Should().NotBeNull();
        
        var tableName = entityType!.GetTableName();
        tableName.Should().Be("products");
        
        var createdAtProperty = entityType.FindProperty(nameof(IEntityTimeMark.CreatedAt));
        createdAtProperty.Should().NotBeNull();
        createdAtProperty!.GetDefaultValueSql().Should().Be("NOW() AT TIME ZONE 'UTC'");
    }

    [Test]
    public void ApplyPostgreSqlDataModelConventions_MultipleEntitiesWithTimeMark_ShouldSetDefaultValueForAll()
    {
        using var context = new TestDbContext(_options);
        var modelBuilder = new ModelBuilder();
        
        modelBuilder.Entity<ExampleEntity>();
        modelBuilder.Entity<TestUser>();
        modelBuilder.Entity<ProductEntity>();
        
        modelBuilder.ApplyPostgreSqlDataModelConventions();

        var exampleEntityType = modelBuilder.Model.FindEntityType(typeof(ExampleEntity));
        var testUserEntityType = modelBuilder.Model.FindEntityType(typeof(TestUser));
        var productEntityType = modelBuilder.Model.FindEntityType(typeof(ProductEntity));
        
        exampleEntityType.Should().NotBeNull();
        testUserEntityType.Should().NotBeNull();
        productEntityType.Should().NotBeNull();
        
        var exampleCreatedAt = exampleEntityType!.FindProperty(nameof(IEntityTimeMark.CreatedAt));
        var userCreatedAt = testUserEntityType!.FindProperty(nameof(IEntityTimeMark.CreatedAt));
        var productCreatedAt = productEntityType!.FindProperty(nameof(IEntityTimeMark.CreatedAt));
        
        exampleCreatedAt!.GetDefaultValueSql().Should().Be("NOW() AT TIME ZONE 'UTC'");
        userCreatedAt!.GetDefaultValueSql().Should().Be("NOW() AT TIME ZONE 'UTC'");
        productCreatedAt!.GetDefaultValueSql().Should().Be("NOW() AT TIME ZONE 'UTC'");
    }
}