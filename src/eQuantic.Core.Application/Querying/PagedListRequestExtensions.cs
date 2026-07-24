using eQuantic.Core.Data.Repository;
using eQuantic.Core.Data.Repository.Options;
using eQuantic.Core.Domain.Entities.Requests;

namespace eQuantic.Core.Application.Querying;

/// <summary>
/// Bridges the query-string request model (<see cref="PagedListRequest{TEntity}" />, whose <c>filterBy</c> and
/// <c>orderBy</c> are parsed by eQuantic.Linq) to the native <see cref="QueryOptions{TEntity}" /> the
/// eQuantic.Core.Data engine executes over DataModels. Because both sides are built on eQuantic.Linq, the
/// filter compiles to the same predicate and the sorts pass straight through.
/// </summary>
public static class PagedListRequestExtensions
{
    /// <summary>Turns the request's filter, sort and include fields into a native <see cref="QueryOptions{TEntity}" />.</summary>
    /// <typeparam name="TEntity">The queried type (typically the DataModel).</typeparam>
    /// <param name="request">The paged list request.</param>
    /// <returns>The query options the read repositories accept.</returns>
    public static QueryOptions<TEntity> ToQueryOptions<TEntity>(this PagedListRequest<TEntity> request)
        where TEntity : class
    {
        var options = new QueryOptions<TEntity>();

        var predicate = request.GetFilterPredicate();
        if (predicate is not null)
        {
            options.Where(predicate);
        }

        var sorts = request.GetSorts();
        if (sorts.Count > 0)
        {
            options.OrderBy(sorts.ToArray());
        }

        if (request.IncludeFields is { Length: > 0 })
        {
            options.Include(request.IncludeFields);
        }

        return options;
    }

    /// <summary>Turns the request's paging into a native <see cref="PageRequest" />.</summary>
    /// <typeparam name="TEntity">The queried type.</typeparam>
    /// <param name="request">The paged list request.</param>
    /// <returns>The page request the paged reads accept.</returns>
    public static PageRequest ToPageRequest<TEntity>(this PagedListRequest<TEntity> request) =>
        new(request.PageIndex ?? 1, request.PageSize ?? PageRequest.DefaultPageSize);
}
