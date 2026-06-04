using System.ComponentModel.DataAnnotations;
using PennyWise.Domain.Enums;

namespace PennyWise.API.DTOs;

// ── Transaction DTOs ────────────────────────────────────────────

public record CreateTransactionRequest(
    [Required, Range(0.01, double.MaxValue, ErrorMessage = "Amount must be positive")] decimal Amount,
    [StringLength(200)] string Description,
    [Required, StringLength(50)] string Category,
    [Required] TransactionType Type,
    [Required] DateTime TransactionDate);

public record UpdateTransactionRequest(
    [Required, Range(0.01, double.MaxValue, ErrorMessage = "Amount must be positive")] decimal Amount,
    [StringLength(200)] string Description,
    [Required, StringLength(50)] string Category,
    [Required] TransactionType Type,
    [Required] DateTime TransactionDate);

public record TransactionResponse(
    Guid Id,
    decimal Amount,
    string Description,
    string Category,
    TransactionType Type,
    DateTime TransactionDate,
    DateTime CreatedAt);

public record TransactionSummaryResponse(
    decimal TotalIncome,
    decimal TotalExpenses,
    decimal NetBalance,
    int TransactionCount);

// ── Budget DTOs ─────────────────────────────────────────────────

public record CreateBudgetRequest(
    [Required, StringLength(50)] string Category,
    [Required, Range(0.01, double.MaxValue, ErrorMessage = "Limit must be positive")] decimal LimitAmount,
    [Required, Range(1, 12)] int Month,
    [Required, Range(2000, 2100)] int Year);

public record UpdateBudgetRequest(
    [Required, StringLength(50)] string Category,
    [Required, Range(0.01, double.MaxValue, ErrorMessage = "Limit must be positive")] decimal LimitAmount,
    [Required, Range(1, 12)] int Month,
    [Required, Range(2000, 2100)] int Year);

public record BudgetResponse(
    Guid Id,
    string Category,
    decimal LimitAmount,
    int Month,
    int Year,
    DateTime CreatedAt);

public record BudgetStatusResponse(
    Guid Id,
    string Category,
    decimal LimitAmount,
    decimal SpentAmount,
    decimal RemainingAmount,
    double PercentUsed,
    int Month,
    int Year);

// ── Pagination ──────────────────────────────────────────────────

public record PaginatedResponse<T>(
    IEnumerable<T> Items,
    int TotalCount,
    int Page,
    int PageSize);
