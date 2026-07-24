using eQuantic.Core.DataModel;
using eQuantic.Core.Domain.Entities;

namespace eQuantic.Core.Persistence.Tests.TestEntities;

public class TestEntity : EntityDataBase, IEntityTimeMark, IEntityTimeTrack, IEntityTimeEnded, IEntityOwned, IEntityTrack, IEntityHistory
{
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public string Name { get; set; } = string.Empty;
    public int CreatedById { get; set; }
    public int? UpdatedById { get; set; }
    public int? DeletedById { get; set; }
}

public class TestGenericEntity : EntityDataBase, IEntityOwned<TestUser, int>, IEntityTrack<TestUser, int>, IEntityHistory<TestUser, int>
{
    private int _createdById;
    public string CreatedById { get; set; } = string.Empty;
    public TestUser? CreatedBy { get; set; }

    public TestUser? UpdatedBy { get; set; }

    public TestUser? DeletedBy { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    int IEntityOwned<int>.CreatedById
    {
        get => _createdById;
        set => _createdById = value;
    }

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