using System.Text.Json;
using eQuantic.Core.CQS.Abstractions.Outbox;
using eQuantic.Core.Data.Repository;
using eQuantic.Core.DomainEvents;
using eQuantic.Core.Eventing;
using eQuantic.Core.Persistence.Repositories;

namespace eQuantic.Core.Persistence;

/// <summary>
/// The unit of work application code commits after mutating aggregates through an
/// <see cref="IAggregateRepository{TAggregate,TData,TKey}" />. Committing it persists the aggregates'
/// DataModels <b>and</b> records their domain events in the outbox in one transaction.
/// </summary>
public interface IDomainUnitOfWork
{
    /// <summary>Persists the staged DataModels and their aggregates' domain events atomically.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of write operations flushed.</returns>
    Task<int> CommitAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// The application-layer unit of work that turns domain events + outbox into a single atomic write. On
/// <see cref="CommitAsync" /> it drains the uncommitted domain events from every aggregate written this unit of
/// work, stages each as an outbox message, and then commits the native unit of work — so the aggregates'
/// DataModels and the outbox rows land in <b>one transaction</b>. Delivery is then the outbox relay's job, so
/// events are never lost to a crash between "saved the aggregate" and "published the event", on any store.
/// </summary>
public sealed class DomainEventDispatchingUnitOfWork : IDomainUnitOfWork
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IUnitOfWork _inner;
    private readonly IAggregateTracker _tracker;
    private readonly IOutboxRepository _outbox;

    /// <summary>Initializes the unit of work over the native unit of work, the aggregate tracker and the outbox.</summary>
    public DomainEventDispatchingUnitOfWork(IUnitOfWork inner, IAggregateTracker tracker, IOutboxRepository outbox)
    {
        _inner = inner;
        _tracker = tracker;
        _outbox = outbox;
    }

    /// <inheritdoc />
    public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
    {
        var aggregates = _tracker.DrainTracked();

        var withEvents = new List<IAggregateRoot>();
        foreach (var aggregate in aggregates)
        {
            var raised = false;
            foreach (var domainEvent in aggregate.GetUncommittedEvents())
            {
                // stages on the SAME native unit of work as the aggregate DataModel write
                await _outbox.Add(ToMessage(domainEvent), cancellationToken).ConfigureAwait(false);
                raised = true;
            }

            if (raised)
            {
                withEvents.Add(aggregate);
            }
        }

        var flushed = await _inner.CommitAsync(cancellationToken).ConfigureAwait(false);

        foreach (var aggregate in withEvents)
        {
            aggregate.ClearUncommittedEvents();
        }

        return flushed;
    }

    private static IOutboxMessage ToMessage(IEvent domainEvent)
    {
        var type = domainEvent.GetType();
        return new OutboxMessage
        {
            MessageType = type.AssemblyQualifiedName ?? type.FullName ?? type.Name,
            Payload = JsonSerializer.Serialize(domainEvent, type, SerializerOptions),
            CorrelationId = domainEvent.EventId.ToString(),
        };
    }
}
