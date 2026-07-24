using System.Reflection;
using eQuantic.Linq.Expressions;
using eQuantic.Linq.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace eQuantic.Core.Domain.Entities;

/// <summary>
/// Domain-flavored alias of the eQuantic.Linq family <see cref="QueryFilterCollection{TEntity}"/>:
/// parsing lives in the family package (the static <c>TryParse</c> binds natively in MVC and
/// Minimal APIs); this type preserves the Core.Api vocabulary and adds the multi-value
/// <see cref="BindAsync"/> convenience for direct Minimal API parameters.
/// </summary>
/// <typeparam name="TEntity">Root entity the filter expressions are anchored on.</typeparam>
public class FilteringCollection<TEntity> : QueryFilterCollection<TEntity>
{
    public FilteringCollection()
    {
    }

    public FilteringCollection(IEnumerable<ExpressionModel<TEntity>> collection) : base(collection)
    {
    }

    /// <summary>Parses a single query-string value (used by MVC and attribute-bound members).</summary>
    /// <param name="value">Raw filter expression.</param>
    /// <param name="provider">Unused; required by the binding contract.</param>
    /// <param name="filteringCollection">Parsed collection, or null when the value is invalid or empty.</param>
    public static bool TryParse(string? value, IFormatProvider? provider,
        out FilteringCollection<TEntity>? filteringCollection)
    {
        if (TryParseValue(value, out var model))
        {
            filteringCollection = [model];
            return true;
        }

        filteringCollection = null;
        return false;
    }

    /// <summary>
    /// Binds every query value of the parameter's key — <c>[FromQuery(Name = "…")]</c> when
    /// present, otherwise <c>filterBy</c> (used by direct Minimal API parameters).
    /// </summary>
    /// <param name="context">Current HTTP context.</param>
    /// <param name="parameter">Bound parameter info.</param>
    public static ValueTask<FilteringCollection<TEntity>?> BindAsync(HttpContext context, ParameterInfo parameter)
    {
        var key = parameter.GetCustomAttribute<FromQueryAttribute>()?.Name ?? "filterBy";
        var collection = new FilteringCollection<TEntity>();
        foreach (var value in context.Request.Query[key])
        {
            if (TryParseValue(value, out var model))
            {
                collection.Add(model);
            }
        }

        return new ValueTask<FilteringCollection<TEntity>?>(collection);
    }
}
