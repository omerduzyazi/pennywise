namespace PennyWise.Domain.Entities;

/// <summary>
/// Represents an investment portfolio belonging to a user.
/// </summary>
public class Portfolio : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Foreign key
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    // Navigation
    public ICollection<Holding> Holdings { get; set; } = new List<Holding>();
}
