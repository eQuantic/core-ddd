using eQuantic.Core.Persistence.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace eQuantic.Core.Persistence.Extensions;

/// <summary>
/// Registration for the application-layer domain-persistence seam: the aggregate tracker, the aggregate
/// repository (domain ↔ DataModel) and the domain unit of work that drains domain events into the outbox.
/// </summary>
public static class DomainPersistenceExtensions
{
    /// <summary>
    /// Registers the aggregate tracker, the <see cref="IAggregateRepository{TAggregate,TData,TKey}" /> and the
    /// <see cref="IDomainUnitOfWork" />. Pair it with <c>services.AddMappers()</c> (eQuantic.Mapper, for the
    /// domain ↔ DataModel mappers), the native provider's repositories for your DataModels, and the native
    /// outbox (<c>IOutboxRepository</c>).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddDomainPersistence(this IServiceCollection services)
    {
        services.AddScoped<IAggregateTracker, AggregateTracker>();
        services.AddScoped(typeof(IAggregateRepository<,,>), typeof(AggregateRepository<,,>));
        services.AddScoped<IDomainUnitOfWork, DomainEventDispatchingUnitOfWork>();
        return services;
    }
}
