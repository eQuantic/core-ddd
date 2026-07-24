namespace eQuantic.Core.Domain.Entities;

/// <summary>
/// The tactical-DDD aggregate root: the consistency boundary of an aggregate and the source of its domain
/// events. It is a <b>pure domain</b> object — it implements <see cref="IDomainEntity{TKey}" /> and knows
/// nothing about persistence (it is never an <c>IEntity</c>/DataModel). Domain events come from
/// <see cref="eQuantic.Core.DomainEvents.AggregateRoot{TKey}" />; equality is by identity (same type + same
/// non-default <see cref="eQuantic.Core.DomainEvents.AggregateRoot{TKey}.Id" />). The application layer maps it
/// to a DataModel for storage.
/// </summary>
/// <typeparam name="TKey">The identifier type.</typeparam>
public abstract class AggregateRoot<TKey>
    : eQuantic.Core.DomainEvents.AggregateRoot<TKey>, IDomainEntity<TKey>, IEquatable<AggregateRoot<TKey>>
{
    /// <inheritdoc />
    public override TKey Id { get; protected set; } = default!;

    /// <summary>Gets the key.</summary>
    /// <returns>The key.</returns>
    public TKey GetKey() => Id;

    /// <summary>Sets the key.</summary>
    /// <param name="key">The key.</param>
    public void SetKey(TKey key) => Id = key;

    /// <summary>A transient aggregate has no identity yet (its key is the default value), so it is compared by reference.</summary>
    public bool IsTransient() => EqualityComparer<TKey>.Default.Equals(Id, default!);

    /// <inheritdoc />
    public bool Equals(AggregateRoot<TKey>? other)
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
    public override bool Equals(object? obj) => Equals(obj as AggregateRoot<TKey>);

    /// <inheritdoc />
    public override int GetHashCode() =>
        IsTransient() ? base.GetHashCode() : EqualityComparer<TKey>.Default.GetHashCode(Id!);

    /// <summary>Identity equality.</summary>
    public static bool operator ==(AggregateRoot<TKey>? left, AggregateRoot<TKey>? right) =>
        left is null ? right is null : left.Equals(right);

    /// <summary>Identity inequality.</summary>
    public static bool operator !=(AggregateRoot<TKey>? left, AggregateRoot<TKey>? right) => !(left == right);
}

/// <summary>A <see cref="Guid" />-keyed <see cref="AggregateRoot{TKey}" /> — the common case.</summary>
public abstract class AggregateRoot : AggregateRoot<Guid>
{
}
