using eQuantic.Core.DomainEvents;

namespace eQuantic.Core.Persistence.Repositories;

/// <summary>
/// Collects the aggregate roots mutated during the current unit of work so their domain events can be drained
/// into the outbox when it commits. Scoped: one tracker per unit of work / request.
/// </summary>
public interface IAggregateTracker
{
    /// <summary>Records an aggregate root that has been staged for persistence.</summary>
    /// <param name="aggregate">The aggregate root.</param>
    void Track(IAggregateRoot aggregate);

    /// <summary>Returns the tracked aggregate roots and clears the tracker.</summary>
    /// <returns>The aggregate roots tracked since the last drain, in insertion order.</returns>
    IReadOnlyList<IAggregateRoot> DrainTracked();
}

/// <summary>
/// The default <see cref="IAggregateTracker" /> — an ordered, reference-deduplicated list of the aggregate
/// roots staged during the current unit of work.
/// </summary>
public sealed class AggregateTracker : IAggregateTracker
{
    private readonly List<IAggregateRoot> _tracked = new();

    /// <inheritdoc />
    public void Track(IAggregateRoot aggregate)
    {
        if (!_tracked.Contains(aggregate))
        {
            _tracked.Add(aggregate);
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<IAggregateRoot> DrainTracked()
    {
        var drained = _tracked.ToArray();
        _tracked.Clear();
        return drained;
    }
}
