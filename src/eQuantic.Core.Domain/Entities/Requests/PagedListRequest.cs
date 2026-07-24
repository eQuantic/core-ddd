using System.Linq.Expressions;
using eQuantic.Linq.Web;
using Microsoft.AspNetCore.Mvc;

namespace eQuantic.Core.Domain.Entities.Requests;

/// <summary>
/// The paged list request class. Filtering and ordering bind from the query string with the
/// eQuantic.Linq v3 syntax (e.g. <c>filterBy=total:gt(100),status:eq(Paid)</c>,
/// <c>orderBy=total:desc,customer.name</c>) into typed, serializable collections.
/// </summary>
public class PagedListRequest<TEntity> : BasicRequest, IGetRequest
{
    /// <summary>
    /// Gets or sets the value of the page index
    /// </summary>
    [FromQuery]
    public int? PageIndex { get; set; }

    /// <summary>
    /// Gets or sets the value of the page size
    /// </summary>
    [FromQuery]
    public int? PageSize { get; set; }

    /// <summary>
    /// Gets or sets the value of the filtering (typed expression models; items combine with AND)
    /// </summary>
    [FromQuery]
    public FilteringCollection<TEntity>? FilterBy { get; set; }

    /// <summary>
    /// Gets or sets the value of the sorting
    /// </summary>
    [FromQuery]
    public SortingCollection<TEntity>? OrderBy { get; set; }

    /// <summary>
    /// Gets or sets the include fields
    /// </summary>
    [FromQuery]
    public string[]? IncludeFields { get; set; }

    public PagedListRequest()
    {
    }

    public PagedListRequest(
        int? pageIndex,
        int? pageSize,
        FilteringCollection<TEntity>? filterBy,
        SortingCollection<TEntity>? orderBy,
        string[]? includeFields = null)
    {
        if (pageIndex.HasValue) PageIndex = pageIndex;
        if (pageSize.HasValue) PageSize = pageSize;
        if (filterBy != null) FilterBy = filterBy;
        if (orderBy != null) OrderBy = orderBy;
        if (includeFields != null) IncludeFields = includeFields;
    }

    /// <summary>
    /// Combines <see cref="FilterBy"/> into a single typed predicate (null when no filter was supplied).
    /// </summary>
    /// <param name="options">Query-string options; defaults apply when omitted.</param>
    public Expression<Func<TEntity, bool>>? GetFilterPredicate(QueryStringOptions? options = null) =>
        FilterBy is { Count: > 0 } ? FilterBy.ToPredicate(options) : null;

    /// <summary>
    /// The typed sort expressions (empty when none supplied).
    /// </summary>
    public IReadOnlyList<QuerySort<TEntity>> GetSorts() =>
        OrderBy is { Count: > 0 } ? OrderBy : [];
}

/// <summary>
/// The referenced paged list request class
/// </summary>
public class PagedListRequest<TEntity, TReferenceKey> : PagedListRequest<TEntity>, IReferencedRequest<TReferenceKey>
{
    private TReferenceKey? _referenceId;

    public PagedListRequest()
    {
    }

    public PagedListRequest(
        TReferenceKey referenceId,
        int? pageIndex,
        int? pageSize,
        FilteringCollection<TEntity>? filterBy,
        SortingCollection<TEntity>? orderBy,
        string[]? includeFields = null)
        : base(pageIndex, pageSize, filterBy, orderBy, includeFields)
    {
        _referenceId = referenceId;
    }

    public void SetReferenceId(TReferenceKey referenceId)
    {
        _referenceId = referenceId;
    }

    public TReferenceKey? GetReferenceId()
    {
        return _referenceId;
    }
}
