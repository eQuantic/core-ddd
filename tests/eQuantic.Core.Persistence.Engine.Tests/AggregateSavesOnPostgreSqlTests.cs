using System.Text.Json.Nodes;
using eQuantic.Core.Data.Modeling;
using eQuantic.Core.Data.PostgreSql.Extensions;
using eQuantic.Core.Data.Repository;
using eQuantic.Core.Persistence.Extensions;
using eQuantic.Core.Persistence.Repositories;
using eQuantic.Mapper;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using NSubstitute;
using Testcontainers.PostgreSql;

namespace eQuantic.Core.Persistence.Engine.Tests;

// ---- the aggregate, its DataModel and its table --------------------------------------------------------

public sealed class Parcel : eQuantic.Core.Domain.Entities.AggregateRoot<Guid>
{
    public Parcel(Guid id) => Id = id;

    public string Customer { get; set; } = "";
    public string Status { get; set; } = "";
    public JsonNode? Details { get; set; }
}

[Entity("parcels")]
public sealed class ParcelData : IEntity<Guid>
{
    [EntityKey, StoredAs("id")]
    public Guid Id { get; set; }

    [StoredAs("customer")]
    public string Customer { get; set; } = "";

    [StoredAs("status")]
    public string Status { get; set; } = "";

    [StoredAs("details")]
    public JsonNode? Details { get; set; }

    /// <summary>A column the aggregate does not carry: its mapper leaves it null.</summary>
    [StoredAs("note")]
    public string? Note { get; set; }

    public Guid GetKey() => Id;
    public void SetKey(Guid key) => Id = key;
}

// ---- what a save leaves in the row -----------------------------------------------------------------------

[TestFixture]
[NonParallelizable]
public sealed class AggregateSavesOnPostgreSqlTests
{
    private const string Table = """
        CREATE TABLE parcels (
            id uuid PRIMARY KEY,
            customer text NOT NULL,
            status text NOT NULL,
            details jsonb,
            note text
        )
        """;

    private PostgreSqlContainer? _container;
    private Exception? _startup;
    private ServiceProvider? _provider;
    private string _connectionString = "";

    [OneTimeSetUp]
    public async Task StartAsync()
    {
        try
        {
            _container = new PostgreSqlBuilder("postgres:17-alpine").Build();
            await _container.StartAsync();
        }
        catch (Exception exception)
        {
            _startup = exception;
        }
    }

    [OneTimeTearDown]
    public async Task StopAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    [SetUp]
    public async Task NewDatabaseAsync()
    {
        if (_startup is not null)
        {
            var reason = $"PostgreSQL could not start in a container (Docker required): {_startup.Message}";
            if (Environment.GetEnvironmentVariable("CORE_DDD_ENGINE_TESTS") == "required")
            {
                Assert.Fail(reason);
            }

            Assert.Ignore(reason);
        }

        var name = "db_" + Guid.NewGuid().ToString("N");
        await using (var admin = new NpgsqlConnection(_container!.GetConnectionString()))
        {
            await admin.OpenAsync();
            await using var create = new NpgsqlCommand($"CREATE DATABASE {name}", admin);
            await create.ExecuteNonQueryAsync();
        }

        _connectionString = new NpgsqlConnectionStringBuilder(_container.GetConnectionString()) { Database = name }.ConnectionString;
        await ExecuteAsync(Table);

        var services = new ServiceCollection();
        services.AddPostgreSqlDatabase(_connectionString, model => model.Entity<ParcelData>(_ => { }));
        services.AddPostgreSqlRepositories();
        services.AddSingleton(Mappers());
        services.AddDomainPersistence(options => options.RefuseWholeRowWrites = true);
        _provider = services.BuildServiceProvider();
    }

    [TearDown]
    public async Task DisposeAsync()
    {
        if (_provider is not null)
        {
            await _provider.DisposeAsync();
        }
    }

    // The mappers share the JSON node between the aggregate and the DataModel, as a hand-written mapper often does.
    private static IMapperFactory Mappers()
    {
        var toData = Substitute.For<IMapper<Parcel, ParcelData>>();
        toData.Map(Arg.Any<Parcel>()).Returns(call =>
        {
            var parcel = call.Arg<Parcel>();
            return new ParcelData { Id = parcel.Id, Customer = parcel.Customer, Status = parcel.Status, Details = parcel.Details };
        });
        var toDomain = Substitute.For<IMapper<ParcelData, Parcel>>();
        toDomain.Map(Arg.Any<ParcelData>()).Returns(call =>
        {
            var data = call.Arg<ParcelData>();
            return new Parcel(data.Id) { Customer = data.Customer, Status = data.Status, Details = data.Details };
        });
        var mappers = Substitute.For<IMapperFactory>();
        mappers.GetMapper<Parcel, ParcelData>().Returns(toData);
        mappers.GetMapper<ParcelData, Parcel>().Returns(toDomain);
        return mappers;
    }

    private async Task ExecuteAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task<Guid> SeedAsync()
    {
        var id = Guid.NewGuid();
        await ExecuteAsync(
            $"INSERT INTO parcels (id, customer, status, details, note) VALUES ('{id}', 'acme', 'open', '{{\"weight\":2}}', 'kept by the store')");
        return id;
    }

    private async Task<(string Customer, string Status, string? Details, string? Note)> RowAsync(Guid id)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"SELECT customer, status, details::text, note FROM parcels WHERE id = '{id}'", connection);
        await using var reader = await command.ExecuteReaderAsync();
        (await reader.ReadAsync()).Should().BeTrue();
        return (reader.GetString(0), reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetString(2), reader.IsDBNull(3) ? null : reader.GetString(3));
    }

    private static IAggregateRepository<Parcel, ParcelData, Guid> Parcels(IServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<IAggregateRepository<Parcel, ParcelData, Guid>>();

    private static Task CommitAsync(IServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<IUnitOfWork>().CommitAsync();

    [Test]
    public async Task Two_saves_of_one_row_keep_both_changes()
    {
        var id = await SeedAsync();
        using var first = _provider!.CreateScope();
        using var second = _provider.CreateScope();

        // Both read the row before either writes, as two requests at once do.
        var renamed = (await Parcels(first).GetAsync(id))!;
        var shipped = (await Parcels(second).GetAsync(id))!;

        renamed.Customer = "globex";
        await Parcels(first).ModifyAsync(renamed);
        await CommitAsync(first);

        shipped.Status = "shipped";
        await Parcels(second).ModifyAsync(shipped);
        await CommitAsync(second);

        var row = await RowAsync(id);
        row.Customer.Should().Be("globex");
        row.Status.Should().Be("shipped");
    }

    [Test]
    public async Task A_column_the_aggregate_does_not_carry_keeps_its_value()
    {
        var id = await SeedAsync();
        using var scope = _provider!.CreateScope();

        var parcel = (await Parcels(scope).GetAsync(id))!;
        parcel.Status = "shipped";
        await Parcels(scope).ModifyAsync(parcel);
        await CommitAsync(scope);

        (await RowAsync(id)).Note.Should().Be("kept by the store");
    }

    [Test]
    public async Task A_save_that_changed_nothing_writes_nothing()
    {
        var id = await SeedAsync();
        using var scope = _provider!.CreateScope();

        var parcel = (await Parcels(scope).GetAsync(id))!;
        // Another request changes the row after this one read it.
        await ExecuteAsync($"UPDATE parcels SET status = 'held' WHERE id = '{id}'");
        await Parcels(scope).ModifyAsync(parcel);
        await CommitAsync(scope);

        (await RowAsync(id)).Status.Should().Be("held");
    }

    [Test]
    public async Task A_json_member_changed_in_place_is_written()
    {
        var id = await SeedAsync();
        using var scope = _provider!.CreateScope();

        var parcel = (await Parcels(scope).GetAsync(id))!;
        parcel.Details!["weight"] = 3;
        await Parcels(scope).ModifyAsync(parcel);
        await CommitAsync(scope);

        JsonNode.Parse((await RowAsync(id)).Details!)!["weight"]!.GetValue<int>().Should().Be(3);
    }

    [Test]
    public async Task An_aggregate_the_repository_did_not_load_is_refused()
    {
        var id = await SeedAsync();
        using var scope = _provider!.CreateScope();

        var save = () => Parcels(scope).ModifyAsync(new Parcel(id) { Customer = "initech", Status = "open" });

        await save.Should().ThrowAsync<InvalidOperationException>();
        await CommitAsync(scope);
        (await RowAsync(id)).Customer.Should().Be("acme");
    }
}
