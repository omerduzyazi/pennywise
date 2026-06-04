using PennyWise.Domain.Enums;

namespace PennyWise.Domain.Entities;

/// <summary>
/// Represents a single investment holding within a portfolio.
/// Tracks purchase metadata for TWR calculations.
/// </summary>
public class Holding : BaseEntity
{
    public string TickerSymbol { get; set; } = string.Empty;
    public string InstrumentName { get; set; } = string.Empty;
    public InstrumentType InstrumentType { get; set; }
    public decimal Quantity { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public DateTime PurchaseDate { get; set; }

    // Foreign key
    public Guid PortfolioId { get; set; }
    public Portfolio Portfolio { get; set; } = null!;
}
