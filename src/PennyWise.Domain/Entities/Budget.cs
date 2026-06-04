namespace PennyWise.Domain.Entities;

/// <summary>
/// Represents a monthly budget target for a specific spending category.
/// </summary>
public class Budget : BaseEntity
{
    public string Category { get; set; } = string.Empty;
    public decimal LimitAmount { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }

    // Foreign key
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}
