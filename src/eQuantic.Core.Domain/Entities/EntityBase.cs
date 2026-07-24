namespace eQuantic.Core.Domain.Entities;

public abstract class EntityBase : EntityBase<int>
{
}

public abstract class EntityBase<TKey> : IDomainEntity<TKey>
{
    public TKey Id { get; set; }

    public TKey GetKey()
    {
        return Id;
    }

    public void SetKey(TKey key)
    {
        Id = key;
    }
}