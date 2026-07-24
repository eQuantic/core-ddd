using eQuantic.Core.Domain.Entities;
using eQuantic.Core.Domain.ValueObjects;
using eQuantic.Core.DomainEvents;
using FluentAssertions;

namespace eQuantic.Core.Domain.Tests;

// ---- fixtures ------------------------------------------------------------------------------------

internal sealed class Money(decimal amount, string currency) : ValueObject
{
    public decimal Amount { get; } = amount;
    public string Currency { get; } = currency;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}

internal sealed class Weight(decimal value) : ValueObject
{
    public decimal Value { get; } = value;
    protected override IEnumerable<object?> GetEqualityComponents() { yield return Value; }
}

internal sealed class Customer : EntityBase<Guid>;

internal sealed class OrderPlaced(Guid orderId) : DomainEventBase
{
    public Guid OrderId { get; } = orderId;
}

internal sealed class Order : eQuantic.Core.Domain.Entities.AggregateRoot<Guid>
{
    private Order(Guid id)
    {
        Id = id;   // Id has a protected set — accessible from the aggregate's own constructor
        RaiseDomainEvent(new OrderPlaced(id));
    }

    public static Order Place(Guid id) => new(id);
}

// ---- ValueObject ---------------------------------------------------------------------------------

[TestFixture]
public sealed class ValueObjectTests
{
    [Test]
    public void Equal_by_all_components()
    {
        new Money(10m, "USD").Should().Be(new Money(10m, "USD"));
        (new Money(10m, "USD") == new Money(10m, "USD")).Should().BeTrue();
    }

    [Test]
    public void Not_equal_when_a_component_differs()
    {
        new Money(10m, "USD").Should().NotBe(new Money(10m, "EUR"));
        new Money(10m, "USD").Should().NotBe(new Money(11m, "USD"));
    }

    [Test]
    public void Not_equal_across_types_even_with_same_shape()
    {
        ValueObject money = new Money(10m, "USD");
        ValueObject weight = new Weight(10m);
        money.Should().NotBe(weight);
    }

    [Test]
    public void Equal_value_objects_share_a_hash_code()
    {
        new Money(10m, "USD").GetHashCode().Should().Be(new Money(10m, "USD").GetHashCode());
    }
}

// ---- Entity (identity equality, pure domain) -----------------------------------------------------

[TestFixture]
public sealed class EntityIdentityTests
{
    [Test]
    public void Equal_by_identity_not_by_reference()
    {
        var id = Guid.NewGuid();
        var a = new Customer { Id = id };
        var b = new Customer { Id = id };
        a.Should().Be(b);
        (a == b).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Test]
    public void Different_ids_are_not_equal()
    {
        new Customer { Id = Guid.NewGuid() }.Should().NotBe(new Customer { Id = Guid.NewGuid() });
    }

    [Test]
    public void Transient_entities_are_not_equal_even_with_the_same_default_key()
    {
        new Customer().Should().NotBe(new Customer());
    }

    [Test]
    public void Is_a_domain_entity()
    {
        var customer = new Customer { Id = Guid.NewGuid() };
        customer.Should().BeAssignableTo<IDomainEntity<Guid>>();
        customer.GetKey().Should().Be(customer.Id);
    }
}

// ---- AggregateRoot (events + identity, pure domain — NOT a DataModel) -----------------------------

[TestFixture]
public sealed class AggregateRootTests
{
    [Test]
    public void Raises_and_exposes_uncommitted_domain_events()
    {
        var order = Order.Place(Guid.NewGuid());

        order.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<OrderPlaced>();
        order.GetUncommittedEvents().Should().HaveCount(1);
    }

    [Test]
    public void Clears_domain_events()
    {
        var order = Order.Place(Guid.NewGuid());
        order.ClearDomainEvents();
        order.DomainEvents.Should().BeEmpty();
    }

    [Test]
    public void Is_equal_by_identity()
    {
        var id = Guid.NewGuid();
        Order.Place(id).Should().Be(Order.Place(id));
        Order.Place(Guid.NewGuid()).Should().NotBe(Order.Place(Guid.NewGuid()));
    }

    [Test]
    public void Is_an_aggregate_root_and_a_domain_entity_but_not_a_datamodel()
    {
        var order = Order.Place(Guid.NewGuid());
        order.Should().BeAssignableTo<IAggregateRoot>();
        order.Should().BeAssignableTo<IDomainEntity<Guid>>();
        order.GetKey().Should().Be(order.Id);
    }
}
