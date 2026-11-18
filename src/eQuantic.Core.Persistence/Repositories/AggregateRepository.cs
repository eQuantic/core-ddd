using System.Runtime.CompilerServices;
using eQuantic.Core.Data.Repository;
using eQuantic.Core.Data.Repository.Write;
using eQuantic.Core.Domain.Entities;
using eQuantic.Core.DomainEvents;
using eQuantic.Mapper;

namespace eQuantic.Core.Persistence.Repositories;

/// <summary>
/// A repository scoped to an aggregate root. It is an <b>application-layer</b> object: it speaks the domain
/// (<typeparamref name="TAggregate" />), maps it to a DataModel (<typeparamref name="TData" />) with
/// <c>eQuantic.Mapper</c>, and delegates persistence to the domain-ignorant native repository over the
/// DataModel. On every write it records the aggregate with the <see cref="IAggregateTracker" /> so its domain
/// events are drained into the outbox when the unit of work commits.
/// </summary>
/// <typeparam name="TAggregate">The aggregate root (domain).</typeparam>
/// <typeparam name="TData">The DataModel that stores it (persistence).</typeparam>
/// <typeparam name="TKey">The identifier type.</typeparam>
public interface IAggregateRepository<TAggregate, TData, TKey>
    where TAggregate : class, IDomainEntity<TKey>, IAggregateRoot
    where TData : class, IEntity<TKey>
{
    /// <summary>Loads an aggregate by its identity (reads the DataModel and maps it back to the domain).</summary>
    Task<TAggregate?> GetAsync(TKey id, CancellationToken cancellationToken = default);

    /// <summary>Stages a new aggregate (maps it to a DataModel) and tracks it for domain-event dispatch.</summary>
    Task AddAsync(TAggregate aggregate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stages what changed in an existing aggregate and tracks it for domain-event dispatch. For an aggregate this
    /// repository loaded or added in its scope, only the DataModel members whose mapped value changed since are
    /// written, and nothing when none did; any other aggregate is written whole, or refused when
    /// <see cref="DomainPersistenceOptions.RefuseWholeRowWrites" /> is on.
    /// </summary>
    Task ModifyAsync(TAggregate aggregate, CancellationToken cancellationToken = default);

    /// <summary>Stages the removal of an aggregate and tracks it for domain-event dispatch.</summary>
    Task RemoveAsync(TAggregate aggregate, CancellationToken cancellationToken = default);
}

/// <summary>
/// The default <see cref="IAggregateRepository{TAggregate, TData, TKey}" />: maps domain ↔ DataModel through
/// <see cref="IMapperFactory" /> and stages the DataModel on the native repository. Writes stage on the ambient
/// unit of work exactly as the native repository does; nothing is persisted until that unit of work commits.
/// </summary>
/// <remarks>
/// It remembers what each aggregate it loads or adds maps to, so that <see cref="ModifyAsync" /> writes only the
/// members the aggregate changed, through the native repository's <see cref="IPartialModifyRepository{TEntity,TKey}" />.
/// A whole-row write would put back every column as this aggregate maps it, undoing whatever another request changed
/// in the same row since it was read.
/// </remarks>
/// <typeparam name="TAggregate">The aggregate root (domain).</typeparam>
/// <typeparam name="TData">The DataModel that stores it (persistence).</typeparam>
/// <typeparam name="TKey">The identifier type.</typeparam>
public sealed class AggregateRepository<TAggregate, TData, TKey>
    : IAggregateRepository<TAggregate, TData, TKey>
    where TAggregate : class, IDomainEntity<TKey>, IAggregateRoot
    where TData : class, IEntity<TKey>
{
    private readonly IAsyncRepository<TData, TKey> _repository;
    private readonly IMapperFactory _mappers;
    private readonly IAggregateTracker _tracker;
    private readonly DomainPersistenceOptions _options;

    // What each aggregate mapped to when it was loaded, added or last saved. Weak, so an aggregate the caller dropped
    // is not kept alive for the rest of the scope.
    private readonly ConditionalWeakTable<TAggregate, DataModelSnapshot<TData>> _snapshots = new();

    /// <summary>Initializes the repository over the native DataModel repository, the mapper factory and the tracker.</summary>
    public AggregateRepository(IAsyncRepository<TData, TKey> repository, IMapperFactory mappers, IAggregateTracker tracker)
        : this(repository, mappers, tracker, new DomainPersistenceOptions())
    {
    }

    /// <summary>Initializes the repository over the native DataModel repository, the mapper factory, the tracker and the options.</summary>
    public AggregateRepository(
        IAsyncRepository<TData, TKey> repository,
        IMapperFactory mappers,
        IAggregateTracker tracker,
        DomainPersistenceOptions options)
    {
        _repository = repository;
        _mappers = mappers;
        _tracker = tracker;
        _options = options;
    }

    /// <inheritdoc />
    public async Task<TAggregate?> GetAsync(TKey id, CancellationToken cancellationToken = default)
    {
        var data = await _repository.GetAsync(id, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (data is null)
        {
            return null;
        }

        var aggregate = ToDomain(data);
        // The baseline is what the mapper writes for the aggregate as loaded, not the row itself: a member the mapper
        // normalizes, or one the aggregate does not carry, would otherwise read as changed on every save.
        _snapshots.AddOrUpdate(aggregate, DataModelSnapshot<TData>.Take(ToData(aggregate)));
        return aggregate;
    }

    /// <inheritdoc />
    public async Task AddAsync(TAggregate aggregate, CancellationToken cancellationToken = default)
    {
        var data = ToData(aggregate);
        // Taken before the native repository stamps its lifecycle members on the DataModel.
        var snapshot = DataModelSnapshot<TData>.Take(data);
        await _repository.AddAsync(data, cancellationToken).ConfigureAwait(false);
        _snapshots.AddOrUpdate(aggregate, snapshot);
        // Tracked once its write is staged: an aggregate whose write failed must not publish its events.
        _tracker.Track(aggregate);
    }

    /// <inheritdoc />
    public async Task ModifyAsync(TAggregate aggregate, CancellationToken cancellationToken = default)
    {
        var data = ToData(aggregate);
        var current = DataModelSnapshot<TData>.Take(data);

        if (!_snapshots.TryGetValue(aggregate, out var baseline))
        {
            RefuseWholeRow(
                $"this repository did not load or add this {typeof(TAggregate).Name} in its scope, so it cannot tell which members changed");
        }
        else if (_repository is not IPartialModifyRepository<TData, TKey> partial)
        {
            RefuseWholeRow(
                $"the native repository for {typeof(TData).Name} ({_repository.GetType().Name}) has no member-scoped writes");
        }
        else
        {
            var changed = current.ChangedSince(baseline);
            if (changed.Count > 0)
            {
                await partial.ModifyAsync(data, DataModelSnapshot<TData>.Selector(changed)).ConfigureAwait(false);
            }

            // Only once the write is staged: a stage that throws leaves the change to be written by the next save, and a
            // refused or failed save publishes no events. A save with nothing to write still drains them.
            _snapshots.AddOrUpdate(aggregate, current);
            _tracker.Track(aggregate);
            return;
        }

        await _repository.ModifyAsync(data).ConfigureAwait(false);
        _snapshots.AddOrUpdate(aggregate, current);
        _tracker.Track(aggregate);
    }

    /// <inheritdoc />
    public async Task RemoveAsync(TAggregate aggregate, CancellationToken cancellationToken = default)
    {
        await _repository.RemoveAsync(ToData(aggregate)).ConfigureAwait(false);
        _snapshots.Remove(aggregate);
        _tracker.Track(aggregate);
    }

    private void RefuseWholeRow(string reason)
    {
        if (_options.RefuseWholeRowWrites)
        {
            throw new InvalidOperationException(
                $"Saving this {typeof(TAggregate).Name} would write its whole row, and {nameof(DomainPersistenceOptions.RefuseWholeRowWrites)} is on: {reason}.");
        }
    }

    private TData ToData(TAggregate aggregate) =>
        _mappers.GetMapper<TAggregate, TData>().Map(aggregate)
        ?? throw new InvalidOperationException($"The mapper from {typeof(TAggregate).Name} to {typeof(TData).Name} returned null.");

    private TAggregate ToDomain(TData data) =>
        _mappers.GetMapper<TData, TAggregate>().Map(data)
        ?? throw new InvalidOperationException($"The mapper from {typeof(TData).Name} to {typeof(TAggregate).Name} returned null.");
}
