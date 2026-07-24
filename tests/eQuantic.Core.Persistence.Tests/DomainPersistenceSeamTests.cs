using eQuantic.Core.CQS.Abstractions.Outbox;
using eQuantic.Core.Data.Repository;
using eQuantic.Core.DomainEvents;
using eQuantic.Core.Persistence;
using eQuantic.Core.Persistence.Repositories;
using eQuantic.Mapper;
using FluentAssertions;
using NSubstitute;

namespace eQuantic.Core.Application.Tests;

// ---- fixtures: a pure-domain aggregate and its DataModel ------------------------------------------

public sealed class OrderPlaced(Guid orderId) : DomainEventBase
{
    public Guid OrderId { get; } = orderId;
}

public sealed class Order : eQuantic.Core.Domain.Entities.AggregateRoot<Guid>
{
    public string Customer { get; private set; } = "";

    private Order(Guid id, string customer)
    {
        Id = id;
        Customer = customer;
        RaiseDomainEvent(new OrderPlaced(id));
    }

    public static Order Place(Guid id, string customer) => new(id, customer);
}

public sealed class OrderData : IEntity<Guid>
{
    public Guid Id { get; set; }
    public string Customer { get; set; } = "";
    public Guid GetKey() => Id;
    public void SetKey(Guid key) => Id = key;
}

// ---- the aggregate repository maps domain → DataModel and tracks the aggregate --------------------

[TestFixture]
public sealed class AggregateRepositoryTests
{
    [Test]
    public async Task Add_maps_the_aggregate_to_its_datamodel_and_tracks_it()
    {
        var native = Substitute.For<IAsyncRepository<OrderData, Guid>>();
        var toData = Substitute.For<IMapper<Order, OrderData>>();
        var mappers = Substitute.For<IMapperFactory>();
        mappers.GetMapper<Order, OrderData>().Returns(toData);
        var tracker = new AggregateTracker();

        var order = Order.Place(Guid.NewGuid(), "acme");
        var data = new OrderData { Id = order.Id, Customer = "acme" };
        toData.Map(order).Returns(data);

        var repository = new AggregateRepository<Order, OrderData, Guid>(native, mappers, tracker);
        await repository.AddAsync(order);

        await native.Received(1).AddAsync(data, Arg.Any<CancellationToken>());
        tracker.DrainTracked().Should().ContainSingle().Which.Should().BeSameAs(order);
    }
}

// ---- the domain unit of work stages events into the outbox, then commits — one transaction --------

[TestFixture]
public sealed class DomainEventDispatchingUnitOfWorkTests
{
    [Test]
    public async Task Drains_domain_events_into_the_outbox_before_committing_then_clears_them()
    {
        var inner = Substitute.For<IUnitOfWork>();
        inner.CommitAsync(Arg.Any<CancellationToken>()).Returns(1);
        var outbox = Substitute.For<IOutboxRepository>();
        var tracker = new AggregateTracker();

        var order = Order.Place(Guid.NewGuid(), "acme");
        tracker.Track(order);

        var unitOfWork = new DomainEventDispatchingUnitOfWork(inner, tracker, outbox);
        var flushed = await unitOfWork.CommitAsync();

        // the event was staged on the outbox…
        await outbox.Received(1).Add(
            Arg.Is<IOutboxMessage>(m => m.MessageType!.Contains(nameof(OrderPlaced))),
            Arg.Any<CancellationToken>());
        // …and only then was the native unit of work committed (one flush)…
        await inner.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        flushed.Should().Be(1);
        // …and the durable events were cleared from the aggregate.
        order.DomainEvents.Should().BeEmpty();
    }

    [Test]
    public async Task Commits_even_when_no_aggregate_raised_an_event()
    {
        var inner = Substitute.For<IUnitOfWork>();
        inner.CommitAsync(Arg.Any<CancellationToken>()).Returns(0);
        var outbox = Substitute.For<IOutboxRepository>();

        var unitOfWork = new DomainEventDispatchingUnitOfWork(inner, new AggregateTracker(), outbox);
        await unitOfWork.CommitAsync();

        await outbox.DidNotReceive().Add(Arg.Any<IOutboxMessage>(), Arg.Any<CancellationToken>());
        await inner.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
