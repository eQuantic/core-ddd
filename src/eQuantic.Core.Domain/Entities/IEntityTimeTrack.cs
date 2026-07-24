namespace eQuantic.Core.Domain.Entities;

public interface IEntityTimeTrack
{
    /// <summary>
    /// Gets or sets the value of the updated on
    /// </summary>
    DateTime? UpdatedAt { get; set; }
}