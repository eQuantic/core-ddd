using eQuantic.Core.DataModel;
using eQuantic.Core.Domain.Entities;
using eQuantic.Core.Persistence.Extensions;
using eQuantic.Core.Persistence.Options;
using eQuantic.Core.Persistence.Tests.TestEntities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace eQuantic.Core.Persistence.Tests.Extensions;

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
    public void ApplyDataModelConventions_WithEntityAuditingDisabled_ShouldNotConfigureAuditing()
    {
        using var context = new TestDbContext(_options);
        var modelBuilder = new ModelBuilder();
        
        modelBuilder.Entity<TestEntity>();
        
        modelBuilder.ApplyDataModelConventions(options =>
        {
            options.EnableEntityAuditing(false);
        });

        var entityType = modelBuilder.Model.FindEntityType(typeof(TestEntity));
        entityType.Should().NotBeNull();
        
        var createdAtProperty = entityType!.FindProperty(nameof(IEntityTimeMark.CreatedAt));
        createdAtProperty.Should().BeNull();
    }

    [Test]
    public void ApplyDataModelConventions_WithEntityAuditingEnabled_ShouldConfigureTimeProperties()
    {
        using var context = new TestDbContext(_options);
        var modelBuilder = new ModelBuilder();
        
        modelBuilder.Entity<TestEntity>();
        
        modelBuilder.ApplyDataModelConventions(options =>
        {
            options.EnableEntityAuditing();
        });

        var entityType = modelBuilder.Model.FindEntityType(typeof(TestEntity));
        entityType.Should().NotBeNull();
        
        var createdAtProperty = entityType!.FindProperty(nameof(IEntityTimeMark.CreatedAt));
        createdAtProperty.Should().NotBeNull();
        createdAtProperty!.IsNullable.Should().BeFalse();
        
        var updatedAtProperty = entityType.FindProperty(nameof(IEntityTimeTrack.UpdatedAt));
        updatedAtProperty.Should().NotBeNull();
        updatedAtProperty!.IsNullable.Should().BeTrue();
        
        var deletedAtProperty = entityType.FindProperty(nameof(IEntityTimeEnded.DeletedAt));
        deletedAtProperty.Should().NotBeNull();
        deletedAtProperty!.IsNullable.Should().BeTrue();
    }

    [Test]
    public void ApplyDataModelConventions_WithEntityAuditingEnabled_ShouldConfigureUserAuditProperties()
    {
        using var context = new TestDbContext(_options);
        var modelBuilder = new ModelBuilder();
        
        modelBuilder.Entity<TestEntity>();
        
        modelBuilder.ApplyDataModelConventions(options =>
        {
            options.EnableEntityAuditing();
        });

        var entityType = modelBuilder.Model.FindEntityType(typeof(TestEntity));
        entityType.Should().NotBeNull();
        
        var createdByIdProperty = entityType!.FindProperty(nameof(IEntityOwned.CreatedById));
        createdByIdProperty.Should().NotBeNull();
        createdByIdProperty!.IsNullable.Should().BeFalse();
        
        var updatedByIdProperty = entityType.FindProperty(nameof(IEntityTrack.UpdatedById));
        updatedByIdProperty.Should().NotBeNull();
        updatedByIdProperty!.IsNullable.Should().BeTrue();
        
        var deletedByIdProperty = entityType.FindProperty(nameof(IEntityHistory.DeletedById));
        deletedByIdProperty.Should().NotBeNull();
        deletedByIdProperty!.IsNullable.Should().BeTrue();
    }

    [Test]
    public void ApplyDataModelConventions_WithGenericEntityInterfaces_ShouldConfigureUserAuditProperties()
    {
        using var context = new TestDbContext(_options);
        var modelBuilder = new ModelBuilder();
        
        modelBuilder.Entity<TestGenericEntity>();
        modelBuilder.Entity<TestUser>();
        
        modelBuilder.ApplyDataModelConventions(options =>
        {
            options.EnableEntityAuditing();
        });

        var entityType = modelBuilder.Model.FindEntityType(typeof(TestGenericEntity));
        entityType.Should().NotBeNull();
        
        var createdByIdProperty = entityType!.FindProperty(nameof(IEntityOwned.CreatedById));
        createdByIdProperty.Should().NotBeNull();
        createdByIdProperty!.IsNullable.Should().BeFalse();
        
        var updatedByIdProperty = entityType.FindProperty(nameof(IEntityTrack.UpdatedById));
        updatedByIdProperty.Should().NotBeNull();
        updatedByIdProperty!.IsNullable.Should().BeTrue();
        
        var deletedByIdProperty = entityType.FindProperty(nameof(IEntityHistory.DeletedById));
        deletedByIdProperty.Should().NotBeNull();
        deletedByIdProperty!.IsNullable.Should().BeTrue();
    }

    [Test]
    public void ApplyDataModelConventions_WithoutOptions_ShouldNotConfigureAuditing()
    {
        using var context = new TestDbContext(_options);
        var modelBuilder = new ModelBuilder();
        
        modelBuilder.Entity<TestEntity>();
        
        modelBuilder.ApplyDataModelConventions();

        var entityType = modelBuilder.Model.FindEntityType(typeof(TestEntity));
        entityType.Should().NotBeNull();
    }
}