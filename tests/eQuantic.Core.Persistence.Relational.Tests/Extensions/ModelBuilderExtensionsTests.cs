using eQuantic.Core.DataModel;
using eQuantic.Core.Persistence.Options;
using eQuantic.Core.Persistence.Relational.Extensions;
using eQuantic.Core.Persistence.Relational.Options;
using eQuantic.Core.Persistence.Relational.Tests.TestEntities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace eQuantic.Core.Persistence.Relational.Tests.Extensions;

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
    public void ApplyRelationalDataModelConventions_WithDefaultOptions_ShouldApplyBaseConventions()
    {
        using var context = new TestDbContext(_options);
        var modelBuilder = new ModelBuilder();
        
        modelBuilder.Entity<ExampleEntity>();
        modelBuilder.Entity<TestUser>();
        
        modelBuilder.ApplyRelationalDataModelConventions();

        var entityType = modelBuilder.Model.FindEntityType(typeof(ExampleEntity));
        entityType.Should().NotBeNull();
        
        var tableName = entityType!.GetTableName();
        tableName.Should().Be("ExampleEntities");
    }

    [Test]
    public void ApplyRelationalDataModelConventions_WithSuffixRemoval_ShouldRemoveSuffixFromTableName()
    {
        using var context = new TestDbContext(_options);
        var modelBuilder = new ModelBuilder();
        
        modelBuilder.Entity<ProductEntity>();
        
        modelBuilder.ApplyRelationalDataModelConventions(options =>
        {
            options.RemoveEntitySuffixFromTableName("Entity");
        });

        var entityType = modelBuilder.Model.FindEntityType(typeof(ProductEntity));
        entityType.Should().NotBeNull();
        
        var tableName = entityType!.GetTableName();
        tableName.Should().Be("Products");
    }

    [Test]
    public void ApplyRelationalDataModelConventions_WithPascalCase_ShouldUsePascalCaseTableNames()
    {
        using var context = new TestDbContext(_options);
        var modelBuilder = new ModelBuilder();
        
        modelBuilder.Entity<ProductEntity>();
        
        modelBuilder.ApplyRelationalDataModelConventions(options =>
        {
            options.RemoveEntitySuffixFromTableName("Entity");
            options.UsePascalCase();
        });

        var entityType = modelBuilder.Model.FindEntityType(typeof(ProductEntity));
        entityType.Should().NotBeNull();
        
        var tableName = entityType!.GetTableName();
        tableName.Should().Be("Products");
    }

    [Test]
    public void ApplyRelationalDataModelConventions_WithCamelCase_ShouldUseCamelCaseTableNames()
    {
        using var context = new TestDbContext(_options);
        var modelBuilder = new ModelBuilder();
        
        modelBuilder.Entity<ProductEntity>();
        
        modelBuilder.ApplyRelationalDataModelConventions(options =>
        {
            options.RemoveEntitySuffixFromTableName("Entity");
            options.UseCamelCase();
        });

        var entityType = modelBuilder.Model.FindEntityType(typeof(ProductEntity));
        entityType.Should().NotBeNull();
        
        var tableName = entityType!.GetTableName();
        tableName.Should().Be("products");
    }

    [Test]
    public void ApplyRelationalDataModelConventions_WithSnakeCase_ShouldUseSnakeCaseTableNames()
    {
        using var context = new TestDbContext(_options);
        var modelBuilder = new ModelBuilder();
        
        modelBuilder.Entity<ProductEntity>();
        
        modelBuilder.ApplyRelationalDataModelConventions(options =>
        {
            options.RemoveEntitySuffixFromTableName("Entity");
            options.UseSnakeCase();
        });

        var entityType = modelBuilder.Model.FindEntityType(typeof(ProductEntity));
        entityType.Should().NotBeNull();
        
        var tableName = entityType!.GetTableName();
        tableName.Should().Be("products");
    }

    [Test]
    public void ApplyRelationalDataModelConventions_WithFullyQualifiedPrimaryKeys_ShouldConfigurePrimaryKeyColumnNames()
    {
        using var context = new TestDbContext(_options);
        var modelBuilder = new ModelBuilder();
        
        modelBuilder.Entity<ProductEntity>().HasKey(o => o.Id);
        
        modelBuilder.ApplyRelationalDataModelConventions(options =>
        {
            options.RemoveEntitySuffixFromTableName("Entity");
            options.UseFullyQualifiedPrimaryKeys();
            options.UsePascalCase();
        });

        var entityType = modelBuilder.Model.FindEntityType(typeof(ProductEntity));
        entityType.Should().NotBeNull();
        
        var idProperty = entityType!.FindProperty(nameof(EntityDataBase.Id));
        idProperty.Should().NotBeNull();
        idProperty!.GetColumnName().Should().Be("ProductId");
    }

    [Test]
    public void ApplyRelationalDataModelConventions_WithFullyQualifiedPrimaryKeysAndSnakeCase_ShouldConfigureSnakeCasePrimaryKeyColumnNames()
    {
        using var context = new TestDbContext(_options);
        var modelBuilder = new ModelBuilder();
        
        modelBuilder.Entity<ProductEntity>().HasKey(o => o.Id);
        
        modelBuilder.ApplyRelationalDataModelConventions(options =>
        {
            options.RemoveEntitySuffixFromTableName("Entity");
            options.UseFullyQualifiedPrimaryKeys();
            options.UseSnakeCase();
        });

        var entityType = modelBuilder.Model.FindEntityType(typeof(ProductEntity));
        entityType.Should().NotBeNull();
        
        var idProperty = entityType!.FindProperty(nameof(EntityDataBase.Id));
        idProperty.Should().NotBeNull();
        idProperty!.GetColumnName().Should().Be("product_id");
    }

    [Test]
    public void ApplyRelationalDataModelConventions_WithUserRelationships_ShouldConfigureForeignKeys()
    {
        using var context = new TestDbContext(_options);
        var modelBuilder = new ModelBuilder();
        
        modelBuilder.Entity<ExampleEntity>().HasKey(o => o.Id);
        modelBuilder.Entity<TestUser>().HasKey(o => o.Id);
        
        modelBuilder.ApplyRelationalDataModelConventions();

        var entityType = modelBuilder.Model.FindEntityType(typeof(ExampleEntity));
        entityType.Should().NotBeNull();
        
        var foreignKeys = entityType!.GetForeignKeys();
        foreignKeys.Should().HaveCount(3);
        
        var createdByForeignKey = foreignKeys.FirstOrDefault(fk => 
            fk.Properties.Any(p => p.Name == nameof(ExampleEntity.CreatedById)));
        createdByForeignKey.Should().NotBeNull();
        createdByForeignKey!.IsRequired.Should().BeTrue();
        createdByForeignKey.DeleteBehavior.Should().Be(DeleteBehavior.NoAction);
    }

    [Test]
    public void ApplyRelationalDataModelConventions_WithoutUserEntityInModel_ShouldNotThrowException()
    {
        using var context = new TestDbContext(_options);
        var modelBuilder = new ModelBuilder();
        
        modelBuilder.Entity<ExampleEntity>();
        
        var action = () => modelBuilder.ApplyRelationalDataModelConventions();
        
        action.Should().NotThrow();
    }
}