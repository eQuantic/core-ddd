namespace eQuantic.Core.Domain.ValueObjects;

/// <summary>
/// Base class for value objects — domain concepts with no identity, compared by the value of their
/// components rather than by reference. Derive from it and return the members that define equality from
/// <see cref="GetEqualityComponents" />; structural equality, hash code and the equality operators come for
/// free. Value objects are meant to be immutable: expose them through read-only members and construct a new
/// instance to represent a change.
/// </summary>
/// <example>
/// <code>
/// public sealed class Money : ValueObject
/// {
///     public decimal Amount { get; }
///     public string Currency { get; }
///
///     public Money(decimal amount, string currency) => (Amount, Currency) = (amount, currency);
///
///     protected override IEnumerable&lt;object?&gt; GetEqualityComponents()
///     {
///         yield return Amount;
///         yield return Currency;
///     }
/// }
/// </code>
/// </example>
public abstract class ValueObject : IEquatable<ValueObject>
{
    /// <summary>
    /// Returns the components that define this value object's equality, in a stable order. Two value objects
    /// are equal when they are the same type and every component is equal.
    /// </summary>
    /// <returns>The equality components.</returns>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    /// <inheritdoc />
    public bool Equals(ValueObject? other)
    {
        if (other is null || other.GetType() != GetType())
        {
            return false;
        }

        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as ValueObject);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var component in GetEqualityComponents())
        {
            hash.Add(component);
        }

        return hash.ToHashCode();
    }

    /// <summary>Value equality.</summary>
    public static bool operator ==(ValueObject? left, ValueObject? right) =>
        left is null ? right is null : left.Equals(right);

    /// <summary>Value inequality.</summary>
    public static bool operator !=(ValueObject? left, ValueObject? right) => !(left == right);
}
