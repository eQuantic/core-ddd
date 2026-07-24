namespace eQuantic.Core.Domain.Entities.Requests;

public interface IGetRequest
{
    public string[]? IncludeFields { get; set; }
}