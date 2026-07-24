using Microsoft.AspNetCore.Mvc;

namespace eQuantic.Core.Domain.Entities.Requests;

/// <summary>
/// The update request class
/// </summary>
/// <seealso cref="CreateRequest{TBody}"/>
public class CreateRequest<TBody> : BasicRequest
{
    /// <summary>
    /// Gets or sets the value of the body
    /// </summary>
    [FromBody]
    public TBody? Body { get; set; }

    public CreateRequest()
    {
    }

    public CreateRequest(TBody body)
    {
        Body = body;
    }
}

public class CreateRequest<TBody, TReferenceKey> : CreateRequest<TBody>, IReferencedRequest<TReferenceKey>
{
    private TReferenceKey? _referenceId;
    
    public CreateRequest()
    {
    }
    
    public CreateRequest(TReferenceKey referenceId, TBody body) : base(body)
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