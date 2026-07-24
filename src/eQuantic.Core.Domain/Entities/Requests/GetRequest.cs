using Microsoft.AspNetCore.Mvc;

namespace eQuantic.Core.Domain.Entities.Requests;

public class GetRequest : GetRequest<int>
{
}

/// <summary>
/// The update request class
/// </summary>
/// <seealso cref="ItemRequest"/>
public class GetRequest<TKey> : ItemRequest<TKey>, IGetRequest
{
    /// <summary>
    /// Gets or sets the include fields
    /// </summary>
    [FromQuery]
    public string[]? IncludeFields { get; set; }
    
    public GetRequest()
    {
        
    }

    public GetRequest(TKey id, string[]? includeFields = null) : base(id)
    {
        IncludeFields = includeFields;
    }
}

public class GetRequest<TKey, TReferenceKey> : GetRequest<TKey>, IReferencedRequest<TReferenceKey>
{
    private TReferenceKey? _referenceId;
    
    public GetRequest()
    {
    }
    
    public GetRequest(TReferenceKey referenceId, TKey id, string[]? includeFields = null) : base(id, includeFields)
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