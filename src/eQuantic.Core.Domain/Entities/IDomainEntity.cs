namespace eQuantic.Core.Domain.Entities;

public interface IDomainEntity
{
    
}

public interface IDomainEntity<TKey> : IDomainEntity
{
    /// <summary>
    /// Gets the key.
    /// </summary>
    /// <returns>The key</returns>
    TKey GetKey();

    /// <summary>
    /// Sets the key.
    /// </summary>
    /// <param name="key"></param>
    void SetKey(TKey key);
}
