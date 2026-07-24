namespace eQuantic.Core.Domain.Entities.Requests;

public interface IReferencedRequest<TReferenceKey>
{
    void SetReferenceId(TReferenceKey referenceId);
    TReferenceKey? GetReferenceId();
}