using PennyWise.Domain.Enums;

namespace PennyWise.Domain.Entities;

/// <summary>
/// Represents a single financial transaction (income or expense).
/// </summary>
public class Transaction : BaseEntity
{
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public TransactionType Type { get; set; }
    public DateTime TransactionDate { get; set; }

    // Foreign key
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}
