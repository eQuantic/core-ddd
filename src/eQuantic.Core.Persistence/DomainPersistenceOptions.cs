namespace eQuantic.Core.Persistence;

/// <summary>
/// How the domain-persistence seam writes. Configure it through
/// <c>services.AddDomainPersistence(options =&gt; …)</c>.
/// </summary>
public sealed class DomainPersistenceOptions
{
    /// <summary>
    /// When <c>true</c>, <see cref="Repositories.IAggregateRepository{TAggregate,TData,TKey}.ModifyAsync" /> throws
    /// <see cref="InvalidOperationException" /> instead of writing a whole row: for an aggregate the repository did not
    /// load or add in its scope (so it cannot tell which members changed), and over a native repository without
    /// member-scoped writes (<c>IPartialModifyRepository</c>). <c>false</c> by default, which keeps the whole-row
    /// write for both.
    /// </summary>
    /// <remarks>
    /// A whole-row write puts back every column as the aggregate maps it, so it undoes what another request changed
    /// in the same row since the aggregate was read. Turn this on to make that impossible rather than unlikely.
    /// </remarks>
    public bool RefuseWholeRowWrites { get; set; }
}
