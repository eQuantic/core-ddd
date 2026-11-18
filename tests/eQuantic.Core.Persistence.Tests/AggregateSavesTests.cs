using System.Linq.Expressions;
using System.Text.Json.Nodes;
using eQuantic.Core.Data.Modeling;
using eQuantic.Core.Data.Query;
using eQuantic.Core.Data.Repository;
using eQuantic.Core.Data.Repository.Options;
using eQuantic.Core.Data.Repository.Write;
using eQuantic.Core.Persistence.Extensions;
using eQuantic.Core.Persistence.Repositories;
using eQuantic.Mapper;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace eQuantic.Core.Persistence.Tests;

// ---- fixtures: an aggregate whose DataModel has members of every kind a snapshot handles ----------

public sealed class Shipment : eQuantic.Core.Domain.Entities.AggregateRoot<Guid>
{
    public Shipment(Guid id) => Id = id;

    public string Customer { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime PlacedAt { get; set; }
    public JsonNode? Details { get; set; }
    public byte[] Label { get; set; } = [];
    public List<string> Lines { get; set; } = [];
}

public sealed class ShipmentData : IEntity<Guid>
{
    public Guid Id { get; set; }
    public string Customer { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime PlacedAt { get; set; }
    public JsonNode? Details { get; set; }
    public byte[] Label { get; set; } = [];
    public List<string> Lines { get; set; } = [];

    /// <summary>A column the aggregate does not carry: its mapper leaves it at the default.</summary>
    public string? Note { get; set; }

    /// <summary>Not a column: no provider stores it, so no save may name it.</summary>
    [Unmapped]
    public string Display { get; set; } = "";

    /// <summary>A navigation: another row, never a member of this one.</summary>
    public CarrierData? Carrier { get; set; }

    public Guid GetKey() => Id;
    public void SetKey(Guid key) => Id = key;
}

public sealed class CarrierData : IEntity<Guid>
{
    public Guid Id { get; set; }
    public Guid GetKey() => Id;
    public void SetKey(Guid key) => Id = key;
}

// ---- what ModifyAsync stages -------------------------------------------------------------------------

[TestFixture]
public sealed class AggregateSavesTests
{
    private IAsyncRepository<ShipmentData, Guid> _native = null!;
    private IPartialModifyRepository<ShipmentData, Guid> _partial = null!;
    private IMapperFactory _mappers = null!;
    private AggregateTracker _tracker = null!;
    private readonly List<string[]> _partialWrites = [];
    private bool _failNextStage;

    [SetUp]
    public void SetUp()
    {
        _native = Substitute.For<IAsyncRepository<ShipmentData, Guid>, IPartialModifyRepository<ShipmentData, Guid>>();
        _partial = (IPartialModifyRepository<ShipmentData, Guid>)_native;
        _partialWrites.Clear();
        _failNextStage = false;
        _partial.ModifyAsync(Arg.Any<ShipmentData>(), Arg.Any<Expression<Func<ShipmentData, object?>>>())
            .Returns(call =>
            {
                if (_failNextStage)
                {
                    _failNextStage = false;
                    throw new InvalidOperationException("the unit of work is gone");
                }

                _partialWrites.Add(MemberSelection.Names(call.Arg<Expression<Func<ShipmentData, object?>>>()).ToArray());
                return Task.CompletedTask;
            });

        _mappers = Substitute.For<IMapperFactory>();
        var toData = Substitute.For<IMapper<Shipment, ShipmentData>>();
        toData.Map(Arg.Any<Shipment>()).Returns(call => ToData(call.Arg<Shipment>()));
        var toDomain = Substitute.For<IMapper<ShipmentData, Shipment>>();
        toDomain.Map(Arg.Any<ShipmentData>()).Returns(call => ToDomain(call.Arg<ShipmentData>()));
        _mappers.GetMapper<Shipment, ShipmentData>().Returns(toData);
        _mappers.GetMapper<ShipmentData, Shipment>().Returns(toDomain);
        _tracker = new AggregateTracker();
    }

    [TearDown]
    public void TearDown() => _native.Dispose();

    // The mappers share the JSON node, the byte array and the list between the aggregate and the DataModel, as a
    // hand-written mapper often does: a snapshot that kept the reference would change along with the aggregate.
    private static ShipmentData ToData(Shipment shipment) => new()
    {
        Id = shipment.Id,
        Customer = shipment.Customer,
        Status = shipment.Status,
        PlacedAt = shipment.PlacedAt,
        Details = shipment.Details,
        Label = shipment.Label,
        Lines = shipment.Lines,
    };

    private static Shipment ToDomain(ShipmentData data) => new(data.Id)
    {
        Customer = data.Customer,
        Status = data.Status,
        PlacedAt = data.PlacedAt,
        Details = data.Details,
        Label = data.Label,
        Lines = data.Lines,
    };

    private AggregateRepository<Shipment, ShipmentData, Guid> Repository(bool refuseWholeRowWrites = false) =>
        new(_native, _mappers, _tracker, new DomainPersistenceOptions { RefuseWholeRowWrites = refuseWholeRowWrites });

    private ShipmentData Stored(Guid? id = null)
    {
        var data = new ShipmentData
        {
            Id = id ?? Guid.NewGuid(),
            Customer = "acme",
            Status = "open",
            PlacedAt = new DateTime(2026, 11, 18, 10, 0, 0, DateTimeKind.Utc),
            Details = JsonNode.Parse("""{"weight":2,"tags":["fragile"]}"""),
            Label = [1, 2, 3],
            Lines = ["a", "b"],
            Note = "kept by the store",
            Display = "acme (open)",
            Carrier = new CarrierData { Id = Guid.NewGuid() },
        };
        _native.GetAsync(data.Id, Arg.Any<QueryOptions<ShipmentData>?>(), Arg.Any<CancellationToken>()).Returns(data);
        return data;
    }

    private async Task<(AggregateRepository<Shipment, ShipmentData, Guid> Repository, Shipment Shipment)> Loaded(bool refuse = false)
    {
        var repository = Repository(refuse);
        var shipment = await repository.GetAsync(Stored().Id);
        return (repository, shipment!);
    }

    [Test]
    public async Task A_save_writes_only_the_member_the_aggregate_changed()
    {
        var (repository, shipment) = await Loaded();

        shipment.Customer = "globex";
        await repository.ModifyAsync(shipment);

        _partialWrites.Should().ContainSingle().Which.Should().Equal("Customer");
        await _native.DidNotReceive().ModifyAsync(Arg.Any<ShipmentData>());
        _tracker.DrainTracked().Should().ContainSingle().Which.Should().BeSameAs(shipment);
    }

    [Test]
    public async Task Several_changed_members_are_written_in_one_update()
    {
        var (repository, shipment) = await Loaded();

        shipment.Status = "shipped";
        shipment.Customer = "globex";
        await repository.ModifyAsync(shipment);

        _partialWrites.Should().ContainSingle().Which.Should().Equal("Customer", "Status");
    }

    [Test]
    public async Task A_save_that_changed_nothing_writes_nothing_and_still_tracks_the_aggregate()
    {
        var (repository, shipment) = await Loaded();

        await repository.ModifyAsync(shipment);

        _partialWrites.Should().BeEmpty();
        await _native.DidNotReceive().ModifyAsync(Arg.Any<ShipmentData>());
        _tracker.DrainTracked().Should().ContainSingle().Which.Should().BeSameAs(shipment);
    }

    [Test]
    public async Task A_member_the_aggregate_does_not_carry_is_never_written()
    {
        // The stored Note is "kept by the store" and the mapper writes null for it: a whole-row save would erase it.
        var (repository, shipment) = await Loaded();

        shipment.Status = "shipped";
        await repository.ModifyAsync(shipment);

        _partialWrites.Should().ContainSingle().Which.Should().NotContain("Note");
    }

    [Test]
    public async Task Unmapped_members_and_navigations_are_never_written()
    {
        var (repository, shipment) = await Loaded();

        shipment.Status = "shipped";
        await repository.ModifyAsync(shipment);

        _partialWrites.Should().ContainSingle().Which.Should().NotContain(["Display", "Carrier", "Id"]);
    }

    [Test]
    public async Task A_json_member_changed_in_place_is_written()
    {
        var (repository, shipment) = await Loaded();

        shipment.Details!["weight"] = 3;
        await repository.ModifyAsync(shipment);

        _partialWrites.Should().ContainSingle().Which.Should().Equal("Details");
    }

    [Test]
    public async Task A_json_member_with_the_same_content_in_another_order_is_not_written()
    {
        var (repository, shipment) = await Loaded();

        shipment.Details = JsonNode.Parse("""{"tags":["fragile"],"weight":2}""");
        await repository.ModifyAsync(shipment);

        _partialWrites.Should().BeEmpty();
    }

    [Test]
    public async Task A_byte_array_changed_in_place_is_written()
    {
        var (repository, shipment) = await Loaded();

        shipment.Label[0] = 9;
        await repository.ModifyAsync(shipment);

        _partialWrites.Should().ContainSingle().Which.Should().Equal("Label");
    }

    [Test]
    public async Task A_list_changed_in_place_is_written()
    {
        var (repository, shipment) = await Loaded();

        shipment.Lines.Add("c");
        await repository.ModifyAsync(shipment);

        _partialWrites.Should().ContainSingle().Which.Should().Equal("Lines");
    }

    [Test]
    public async Task A_date_whose_kind_changed_is_written()
    {
        var (repository, shipment) = await Loaded();

        shipment.PlacedAt = DateTime.SpecifyKind(shipment.PlacedAt, DateTimeKind.Unspecified);
        await repository.ModifyAsync(shipment);

        _partialWrites.Should().ContainSingle().Which.Should().Equal("PlacedAt");
    }

    [Test]
    public async Task A_second_save_writes_what_changed_since_the_first()
    {
        var (repository, shipment) = await Loaded();

        shipment.Customer = "globex";
        await repository.ModifyAsync(shipment);
        shipment.Status = "shipped";
        await repository.ModifyAsync(shipment);

        _partialWrites.Should().HaveCount(2);
        _partialWrites[1].Should().Equal("Status");
    }

    [Test]
    public async Task A_stage_that_failed_leaves_the_change_for_the_next_save()
    {
        var (repository, shipment) = await Loaded();
        _failNextStage = true;

        shipment.Customer = "globex";
        var failed = () => repository.ModifyAsync(shipment);
        await failed.Should().ThrowAsync<InvalidOperationException>();

        await repository.ModifyAsync(shipment);

        _partialWrites.Should().ContainSingle().Which.Should().Equal("Customer");
    }

    [Test]
    public async Task An_added_aggregate_saved_again_writes_what_changed_since_it_was_added()
    {
        var repository = Repository();
        var shipment = new Shipment(Guid.NewGuid()) { Customer = "acme", Status = "open" };
        await repository.AddAsync(shipment);

        shipment.Status = "shipped";
        await repository.ModifyAsync(shipment);

        await _native.Received(1).AddAsync(Arg.Any<ShipmentData>(), Arg.Any<CancellationToken>());
        _partialWrites.Should().ContainSingle().Which.Should().Equal("Status");
    }

    [Test]
    public async Task An_aggregate_the_repository_did_not_load_is_written_whole_by_default()
    {
        var repository = Repository();
        var shipment = new Shipment(Guid.NewGuid()) { Customer = "acme" };

        await repository.ModifyAsync(shipment);

        await _native.Received(1).ModifyAsync(Arg.Is<ShipmentData>(data => data.Id == shipment.Id));
        _partialWrites.Should().BeEmpty();
    }

    [Test]
    public async Task An_aggregate_the_repository_did_not_load_is_refused_when_whole_row_writes_are()
    {
        var repository = Repository(refuseWholeRowWrites: true);
        var shipment = new Shipment(Guid.NewGuid()) { Customer = "acme" };

        var save = () => repository.ModifyAsync(shipment);

        (await save.Should().ThrowAsync<InvalidOperationException>())
            .Which.Message.Should().Contain(nameof(Shipment)).And.Contain("did not load or add");
        await _native.DidNotReceive().ModifyAsync(Arg.Any<ShipmentData>());
        _partialWrites.Should().BeEmpty();
    }

    [Test]
    public async Task A_removed_aggregate_is_forgotten()
    {
        var (repository, shipment) = await Loaded(refuse: true);

        await repository.RemoveAsync(shipment);
        var save = () => repository.ModifyAsync(shipment);

        await save.Should().ThrowAsync<InvalidOperationException>();
    }

    [Test]
    public async Task A_native_repository_without_member_scoped_writes_writes_whole_by_default_and_refuses_on_request()
    {
        var native = Substitute.For<IAsyncRepository<ShipmentData, Guid>>();
        var data = new ShipmentData { Id = Guid.NewGuid(), Customer = "acme" };
        native.GetAsync(data.Id, Arg.Any<QueryOptions<ShipmentData>?>(), Arg.Any<CancellationToken>()).Returns(data);

        var lenient = new AggregateRepository<Shipment, ShipmentData, Guid>(native, _mappers, _tracker);
        var shipment = (await lenient.GetAsync(data.Id))!;
        shipment.Customer = "globex";
        await lenient.ModifyAsync(shipment);
        await native.Received(1).ModifyAsync(Arg.Is<ShipmentData>(written => written.Customer == "globex"));

        var strict = new AggregateRepository<Shipment, ShipmentData, Guid>(
            native, _mappers, _tracker, new DomainPersistenceOptions { RefuseWholeRowWrites = true });
        var other = (await strict.GetAsync(data.Id))!;
        other.Customer = "initech";
        var save = () => strict.ModifyAsync(other);
        (await save.Should().ThrowAsync<InvalidOperationException>())
            .Which.Message.Should().Contain("no member-scoped writes");
    }
}

// ---- registration ------------------------------------------------------------------------------------

[TestFixture]
public sealed class DomainPersistenceRegistrationTests
{
    [Test]
    public void The_options_reach_the_aggregate_repository_and_every_call_configures_the_same_options()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IAsyncRepository<ShipmentData, Guid>>());
        var mappers = Substitute.For<IMapperFactory>();
        var toData = Substitute.For<IMapper<Shipment, ShipmentData>>();
        toData.Map(Arg.Any<Shipment>()).Returns(call => new ShipmentData { Id = call.Arg<Shipment>().Id });
        mappers.GetMapper<Shipment, ShipmentData>().Returns(toData);
        services.AddSingleton(mappers);

        services.AddDomainPersistence(options => options.RefuseWholeRowWrites = true);
        services.AddDomainPersistence();

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<DomainPersistenceOptions>().RefuseWholeRowWrites.Should().BeTrue();
        services.Count(descriptor => descriptor.ServiceType == typeof(DomainPersistenceOptions)).Should().Be(1);

        using var scope = provider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IAggregateRepository<Shipment, ShipmentData, Guid>>();
        var save = () => repository.ModifyAsync(new Shipment(Guid.NewGuid()));
        save.Should().ThrowAsync<InvalidOperationException>().WithMessage("*did not load or add*").GetAwaiter().GetResult();
    }

    [Test]
    public void Without_options_the_seam_keeps_whole_row_writes()
    {
        var services = new ServiceCollection();
        services.AddDomainPersistence();

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<DomainPersistenceOptions>().RefuseWholeRowWrites.Should().BeFalse();
    }
}
