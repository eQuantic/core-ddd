using Microsoft.AspNetCore.Mvc;

namespace eQuantic.Core.Domain.Entities.Requests;

public class ItemRequest : ItemRequest<int>
{
}

/// <summary>
/// The item request class
/// </summary>
public class ItemRequest<TKey> : BasicRequest
{
    /// <summary>
    /// Gets or sets the value of the id
    /// </summary>
    [FromRoute]
    public TKey Id { get; set; } = default!;

    public ItemRequest()
    {
    }

    public ItemRequest(TKey id)
    {
        Id = id;
    }
}

public class ItemRequest<TKey, TReferenceKey> : ItemRequest<TKey>, IReferencedRequest<TReferenceKey>
{
    private TReferenceKey? _referenceId;

    public ItemRequest()
    {
    }

    public ItemRequest(TReferenceKey referenceId, TKey id) : base(id)
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