namespace eQuantic.Core.Domain.Entities;

public interface IEntityTimeEnded
{
    /// <summary>
    /// Gets or sets the value of the deleted on
    /// </summary>
    DateTime? DeletedAt { get; set; }
}