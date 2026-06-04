using System.ComponentModel.DataAnnotations;
using PennyWise.Domain.Enums;

namespace PennyWise.API.DTOs;

// ── Portfolio DTOs ──────────────────────────────────────────────

public record CreatePortfolioRequest(
    [Required, StringLength(100)] string Name);

public record PortfolioResponse(
    Guid Id,
    string Name,
    DateTime CreatedAt);

public record PortfolioAnalyticsResponse(
    Guid Id,
    string Name,
    decimal TotalValue,
    decimal AbsoluteReturnAmount,
    double TwrPercentage);

// ── Holding DTOs ────────────────────────────────────────────────

public record CreateHoldingRequest(
    [Required, StringLength(20)] string Symbol,
    [Required, StringLength(100)] string Name,
    [Required] InstrumentType InstrumentType,
    [Required, Range(0.0001, double.MaxValue, ErrorMessage = "Price must be positive")] decimal PurchasePrice,
    [Required, Range(0.0001, double.MaxValue, ErrorMessage = "Quantity must be positive")] decimal Quantity,
    [Required] DateTime PurchaseDate);

public record UpdateHoldingPriceRequest(
    [Required, Range(0.0, double.MaxValue, ErrorMessage = "Price cannot be negative")] decimal CurrentPrice);

public record HoldingResponse(
    Guid Id,
    string Symbol,
    string Name,
    InstrumentType InstrumentType,
    decimal PurchasePrice,
    decimal CurrentPrice,
    decimal Quantity,
    DateTime PurchaseDate);
