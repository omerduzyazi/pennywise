using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PennyWise.API.DTOs;
using PennyWise.Domain.Entities;
using PennyWise.Domain.Enums;
using PennyWise.Domain.Interfaces;

namespace PennyWise.API.Controllers;

/// <summary>
/// CRUD controller for financial transactions (income/expense).
/// All endpoints require authentication and scope data to the current user.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TransactionsController : ControllerBase
{
    private readonly IRepository<Transaction> _transactionRepo;

    public TransactionsController(IRepository<Transaction> transactionRepo)
    {
        _transactionRepo = transactionRepo;
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    /// <summary>
    /// GET /api/transactions — List transactions with pagination and filtering.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] TransactionType? type = null,
        [FromQuery] string? category = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var userId = GetUserId();
        var all = await _transactionRepo.FindAsync(t => t.UserId == userId);
        var query = all.AsQueryable();

        if (type.HasValue)
            query = query.Where(t => t.Type == type.Value);
        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(t => t.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
        if (from.HasValue)
            query = query.Where(t => t.TransactionDate >= from.Value);
        if (to.HasValue)
            query = query.Where(t => t.TransactionDate <= to.Value);

        var totalCount = query.Count();
        var items = query
            .OrderByDescending(t => t.TransactionDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => MapToResponse(t))
            .ToList();

        return Ok(new PaginatedResponse<TransactionResponse>(items, totalCount, page, pageSize));
    }

    /// <summary>
    /// GET /api/transactions/{id}
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId = GetUserId();
        var tx = await _transactionRepo.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
        if (tx == null) return NotFound(new { Error = "Transaction not found." });
        return Ok(MapToResponse(tx));
    }

    /// <summary>
    /// POST /api/transactions
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTransactionRequest request)
    {
        if (request.Amount <= 0)
            return BadRequest(new { Error = "Amount must be greater than zero." });
        if (string.IsNullOrWhiteSpace(request.Category))
            return BadRequest(new { Error = "Category is required." });

        var tx = new Transaction
        {
            Amount = request.Amount,
            Description = request.Description ?? string.Empty,
            Category = request.Category.Trim(),
            Type = request.Type,
            TransactionDate = request.TransactionDate.ToUniversalTime(),
            UserId = GetUserId()
        };

        await _transactionRepo.AddAsync(tx);
        await _transactionRepo.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = tx.Id }, MapToResponse(tx));
    }

    /// <summary>
    /// PUT /api/transactions/{id}
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTransactionRequest request)
    {
        var userId = GetUserId();
        var tx = await _transactionRepo.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
        if (tx == null) return NotFound(new { Error = "Transaction not found." });

        if (request.Amount <= 0)
            return BadRequest(new { Error = "Amount must be greater than zero." });

        tx.Amount = request.Amount;
        tx.Description = request.Description ?? string.Empty;
        tx.Category = request.Category?.Trim() ?? tx.Category;
        tx.Type = request.Type;
        tx.TransactionDate = request.TransactionDate.ToUniversalTime();

        _transactionRepo.Update(tx);
        await _transactionRepo.SaveChangesAsync();

        return Ok(MapToResponse(tx));
    }

    /// <summary>
    /// DELETE /api/transactions/{id}
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = GetUserId();
        var tx = await _transactionRepo.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
        if (tx == null) return NotFound(new { Error = "Transaction not found." });

        _transactionRepo.Remove(tx);
        await _transactionRepo.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// GET /api/transactions/summary — Aggregated financial totals.
    /// </summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var userId = GetUserId();
        var transactions = await _transactionRepo.FindAsync(t => t.UserId == userId);
        var list = transactions.ToList();

        var grouped = list.GroupBy(t => t.Type)
                          .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));

        var totalIncome = grouped.GetValueOrDefault(TransactionType.Income, 0m);
        var totalExpenses = grouped.GetValueOrDefault(TransactionType.Expense, 0m);

        return Ok(new TransactionSummaryResponse(
            TotalIncome: totalIncome,
            TotalExpenses: totalExpenses,
            NetBalance: totalIncome - totalExpenses,
            TransactionCount: list.Count));
    }

    private static TransactionResponse MapToResponse(Transaction t) =>
        new(t.Id, t.Amount, t.Description, t.Category, t.Type, t.TransactionDate, t.CreatedAt);
}
