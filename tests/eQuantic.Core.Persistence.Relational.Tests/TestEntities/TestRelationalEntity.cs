using eQuantic.Core.DataModel;
using eQuantic.Core.Domain.Entities;

namespace eQuantic.Core.Persistence.Relational.Tests.TestEntities;

public class ExampleEntity : EntityDataBase, IEntityOwned<TestUser, int>, IEntityTrack<TestUser, int>, IEntityHistory<TestUser, int>
{
    public TestUser? CreatedBy { get; set; }
    public TestUser? UpdatedBy { get; set; }
    public TestUser? DeletedBy { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int CreatedById { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedById { get; set; }
    public DateTime? DeletedAt { get; set; }
    public int? DeletedById { get; set; }
}

public class TestUser : EntityDataBase
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class ProductEntity : EntityDataBase
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public class CategoryEntity : EntityDataBase
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}