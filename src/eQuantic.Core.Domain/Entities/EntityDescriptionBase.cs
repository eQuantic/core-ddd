namespace eQuantic.Core.Domain.Entities;

public class EntityDescriptionBase : EntityDescriptionBase<int>
{
}

public class EntityDescriptionBase<TKey> : EntityBase<TKey>
{
    public string? Description { get; set; }
}