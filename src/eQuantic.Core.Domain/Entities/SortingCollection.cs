using System.Reflection;
using eQuantic.Linq.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace eQuantic.Core.Domain.Entities;

/// <summary>
/// Domain-flavored alias of the eQuantic.Linq family <see cref="QuerySortCollection{TEntity}"/>:
/// parsing lives in the family package (the static <c>TryParse</c> binds natively in MVC and
/// Minimal APIs); this type preserves the Core.Api vocabulary and adds the multi-value
/// <see cref="BindAsync"/> convenience for direct Minimal API parameters.
/// </summary>
/// <typeparam name="TEntity">Root entity the sort expressions are anchored on.</typeparam>
public class SortingCollection<TEntity> : QuerySortCollection<TEntity>
{
    public SortingCollection()
    {
    }

    public SortingCollection(IEnumerable<QuerySort<TEntity>> collection) : base(collection)
    {
    }

    /// <summary>Parses a single query-string value (used by MVC and attribute-bound members).</summary>
    /// <param name="value">Raw ordering expression.</param>
    /// <param name="provider">Unused; required by the binding contract.</param>
    /// <param name="sortingCollection">Parsed collection, or null when the value is invalid or empty.</param>
    public static bool TryParse(string? value, IFormatProvider? provider,
        out SortingCollection<TEntity>? sortingCollection)
    {
        if (TryParseValue(value, out var sorts))
        {
            sortingCollection = new SortingCollection<TEntity>(sorts);
            return true;
        }

        sortingCollection = null;
        return false;
    }

    /// <summary>
    /// Binds every query value of the parameter's key — <c>[FromQuery(Name = "…")]</c> when
    /// present, otherwise <c>orderBy</c> (used by direct Minimal API parameters).
    /// </summary>
    /// <param name="context">Current HTTP context.</param>
    /// <param name="parameter">Bound parameter info.</param>
    public static ValueTask<SortingCollection<TEntity>?> BindAsync(HttpContext context, ParameterInfo parameter)
    {
        var key = parameter.GetCustomAttribute<FromQueryAttribute>()?.Name ?? "orderBy";
        var collection = new SortingCollection<TEntity>();
        foreach (var value in context.Request.Query[key])
        {
            if (TryParseValue(value, out var sorts))
            {
                collection.AddRange(sorts);
            }
        }

        return new ValueTask<SortingCollection<TEntity>?>(collection);
    }
}
