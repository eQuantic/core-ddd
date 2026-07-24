namespace eQuantic.Core.Domain.Entities;

/// <summary>An <see cref="int" />-keyed <see cref="EntityBase{TKey}" />.</summary>
public abstract class EntityBase : EntityBase<int>
{
}

/// <summary>
/// Base class for entities — domain objects defined by their <b>identity</b> rather than their attributes.
/// Two entities are equal when they are the same type and share the same non-default <see cref="Id" />; an
/// entity whose key is still the default value (not yet persisted) is <i>transient</i> and compares only by
/// reference. This is the DDD entity contract: identity equality, not the attribute-by-attribute equality of a
/// <see cref="ValueObjects.ValueObject" />.
/// </summary>
/// <typeparam name="TKey">The identifier type.</typeparam>
public abstract class EntityBase<TKey> : IDomainEntity<TKey>, IEquatable<EntityBase<TKey>>
{
    /// <summary>The entity identifier.</summary>
    public TKey Id { get; set; } = default!;

    /// <summary>Gets the key.</summary>
    /// <returns>The key.</returns>
    public TKey GetKey() => Id;

    /// <summary>Sets the key.</summary>
    /// <param name="key">The key.</param>
    public void SetKey(TKey key) => Id = key;

    /// <summary>A transient entity has no identity yet (its key is the default value), so it is compared by reference.</summary>
    public bool IsTransient() => EqualityComparer<TKey>.Default.Equals(Id, default!);

    /// <inheritdoc />
    public bool Equals(EntityBase<TKey>? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (other.GetType() != GetType() || IsTransient() || other.IsTransient())
        {
            return false;
        }

        return EqualityComparer<TKey>.Default.Equals(Id, other.Id);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as EntityBase<TKey>);

    /// <inheritdoc />
    public override int GetHashCode() =>
        IsTransient() ? base.GetHashCode() : EqualityComparer<TKey>.Default.GetHashCode(Id!);

    /// <summary>Identity equality.</summary>
    public static bool operator ==(EntityBase<TKey>? left, EntityBase<TKey>? right) =>
        left is null ? right is null : left.Equals(right);

    /// <summary>Identity inequality.</summary>
    public static bool operator !=(EntityBase<TKey>? left, EntityBase<TKey>? right) => !(left == right);
}
