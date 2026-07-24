using eQuantic.Core.Application;
using eQuantic.Core.Data.Repository;
using Microsoft.Extensions.DependencyInjection;

namespace eQuantic.Core.Persistence.Extensions;

/// <summary>
/// Wires the native persistence conventions to the application layer. The eQuantic.Core.Data engine already
/// stamps the time-audit members (<c>CreatedAt</c>/<c>UpdatedAt</c>/<c>DeletedAt</c>) and honours soft delete by
/// convention on DataModels; this fills in the remaining piece — <b>who</b> — by resolving the current user
/// from <see cref="IApplicationContext" /> so <c>CreatedById</c>/<c>UpdatedById</c>/<c>DeletedById</c> are
/// stamped too.
/// </summary>
public static class DataConventionsExtensions
{
    /// <summary>
    /// Resolves the auditing user from the ambient <see cref="IApplicationContext" />. Pass it to a provider's
    /// convention callback, e.g. <c>AddPostgreSqlDatabase(cs, model =&gt; …, c =&gt; c.UseApplicationAudit())</c>.
    /// </summary>
    /// <param name="conventions">The native data conventions.</param>
    /// <returns>The conventions, for chaining.</returns>
    public static DataConventions UseApplicationAudit(this DataConventions conventions)
    {
        conventions.CurrentUserId = services =>
        {
            if (services.GetService<IApplicationContext>() is not { } context)
            {
                return null;
            }

            var userId = context.GetCurrentUserIdAsync().GetAwaiter().GetResult();
            return string.IsNullOrEmpty(userId) ? null : userId;
        };

        return conventions;
    }
}
