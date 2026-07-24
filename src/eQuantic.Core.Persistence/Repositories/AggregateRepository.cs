using eQuantic.Core.Data.Repository;
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

    /// <summary>Stages an existing aggregate's current state and tracks it for domain-event dispatch.</summary>
    Task ModifyAsync(TAggregate aggregate, CancellationToken cancellationToken = default);

    /// <summary>Stages the removal of an aggregate and tracks it for domain-event dispatch.</summary>
    Task RemoveAsync(TAggregate aggregate, CancellationToken cancellationToken = default);
}

/// <summary>
/// The default <see cref="IAggregateRepository{TAggregate, TData, TKey}" />: maps domain ↔ DataModel through
/// <see cref="IMapperFactory" /> and stages the DataModel on the native repository. Writes stage on the ambient
/// unit of work exactly as the native repository does; nothing is persisted until that unit of work commits.
/// </summary>
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

    /// <summary>Initializes the repository over the native DataModel repository, the mapper factory and the tracker.</summary>
    public AggregateRepository(IAsyncRepository<TData, TKey> repository, IMapperFactory mappers, IAggregateTracker tracker)
    {
        _repository = repository;
        _mappers = mappers;
        _tracker = tracker;
    }

    /// <inheritdoc />
    public async Task<TAggregate?> GetAsync(TKey id, CancellationToken cancellationToken = default)
    {
        var data = await _repository.GetAsync(id, cancellationToken: cancellationToken).ConfigureAwait(false);
        return data is null ? null : ToDomain(data);
    }

    /// <inheritdoc />
    public Task AddAsync(TAggregate aggregate, CancellationToken cancellationToken = default)
    {
        _tracker.Track(aggregate);
        return _repository.AddAsync(ToData(aggregate), cancellationToken);
    }

    /// <inheritdoc />
    public Task ModifyAsync(TAggregate aggregate, CancellationToken cancellationToken = default)
    {
        _tracker.Track(aggregate);
        return _repository.ModifyAsync(ToData(aggregate));
    }

    /// <inheritdoc />
    public Task RemoveAsync(TAggregate aggregate, CancellationToken cancellationToken = default)
    {
        _tracker.Track(aggregate);
        return _repository.RemoveAsync(ToData(aggregate));
    }

    private TData ToData(TAggregate aggregate) =>
        _mappers.GetMapper<TAggregate, TData>().Map(aggregate)
        ?? throw new InvalidOperationException($"The mapper from {typeof(TAggregate).Name} to {typeof(TData).Name} returned null.");

    private TAggregate ToDomain(TData data) =>
        _mappers.GetMapper<TData, TAggregate>().Map(data)
        ?? throw new InvalidOperationException($"The mapper from {typeof(TData).Name} to {typeof(TAggregate).Name} returned null.");
}
