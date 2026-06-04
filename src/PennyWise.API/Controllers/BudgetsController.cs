using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PennyWise.API.DTOs;
using PennyWise.Domain.Entities;
using PennyWise.Domain.Enums;
using PennyWise.Domain.Interfaces;

namespace PennyWise.API.Controllers;

/// <summary>
/// CRUD controller for monthly budget limits per category.
/// Includes a status endpoint comparing budgets against actual spending.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BudgetsController : ControllerBase
{
    private readonly IRepository<Budget> _budgetRepo;
    private readonly IRepository<Transaction> _transactionRepo;

    public BudgetsController(
        IRepository<Budget> budgetRepo,
        IRepository<Transaction> transactionRepo)
    {
        _budgetRepo = budgetRepo;
        _transactionRepo = transactionRepo;
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    /// <summary>
    /// GET /api/budgets
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = GetUserId();
        var budgets = await _budgetRepo.FindAsync(b => b.UserId == userId);
        var items = budgets
            .OrderByDescending(b => b.Year).ThenByDescending(b => b.Month)
            .Select(b => MapToResponse(b))
            .ToList();
        return Ok(items);
    }

    /// <summary>
    /// POST /api/budgets
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBudgetRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Category))
            return BadRequest(new { Error = "Category is required." });
        if (request.LimitAmount <= 0)
            return BadRequest(new { Error = "Limit amount must be greater than zero." });
        if (request.Month < 1 || request.Month > 12)
            return BadRequest(new { Error = "Month must be between 1 and 12." });

        var userId = GetUserId();

        // Prevent duplicate budget for same category/month/year
        var existing = await _budgetRepo.FirstOrDefaultAsync(b =>
            b.UserId == userId &&
            b.Category == request.Category.Trim() &&
            b.Month == request.Month &&
            b.Year == request.Year);

        if (existing != null)
            return Conflict(new { Error = "A budget for this category/month/year already exists." });

        var budget = new Budget
        {
            Category = request.Category.Trim(),
            LimitAmount = request.LimitAmount,
            Month = request.Month,
            Year = request.Year,
            UserId = userId
        };

        await _budgetRepo.AddAsync(budget);
        await _budgetRepo.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), MapToResponse(budget));
    }

    /// <summary>
    /// PUT /api/budgets/{id}
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBudgetRequest request)
    {
        var userId = GetUserId();
        var budget = await _budgetRepo.FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);
        if (budget == null) return NotFound(new { Error = "Budget not found." });

        if (request.LimitAmount <= 0)
            return BadRequest(new { Error = "Limit amount must be greater than zero." });

        budget.Category = request.Category?.Trim() ?? budget.Category;
        budget.LimitAmount = request.LimitAmount;
        budget.Month = request.Month;
        budget.Year = request.Year;

        _budgetRepo.Update(budget);
        await _budgetRepo.SaveChangesAsync();

        return Ok(MapToResponse(budget));
    }

    /// <summary>
    /// DELETE /api/budgets/{id}
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = GetUserId();
        var budget = await _budgetRepo.FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);
        if (budget == null) return NotFound(new { Error = "Budget not found." });

        _budgetRepo.Remove(budget);
        await _budgetRepo.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// GET /api/budgets/status — Budget vs. actual spending comparison.
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(
        [FromQuery] int? month = null,
        [FromQuery] int? year = null)
    {
        var userId = GetUserId();
        var targetMonth = month ?? DateTime.UtcNow.Month;
        var targetYear = year ?? DateTime.UtcNow.Year;

        var budgets = await _budgetRepo.FindAsync(b =>
            b.UserId == userId && b.Month == targetMonth && b.Year == targetYear);

        var expenses = await _transactionRepo.FindAsync(t =>
            t.UserId == userId &&
            t.Type == TransactionType.Expense &&
            t.TransactionDate.Month == targetMonth &&
            t.TransactionDate.Year == targetYear);

        var expenseList = expenses.ToList();

        var expenseGroups = expenseList
            .GroupBy(e => e.Category, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount), StringComparer.OrdinalIgnoreCase);

        var statuses = budgets.Select(b =>
        {
            var spent = expenseGroups.GetValueOrDefault(b.Category, 0m);

            return new BudgetStatusResponse(
                Id: b.Id,
                Category: b.Category,
                LimitAmount: b.LimitAmount,
                SpentAmount: spent,
                RemainingAmount: b.LimitAmount - spent,
                PercentUsed: b.LimitAmount > 0 ? Math.Round((double)(spent / b.LimitAmount) * 100, 1) : 0,
                Month: b.Month,
                Year: b.Year);
        }).ToList();

        return Ok(statuses);
    }

    private static BudgetResponse MapToResponse(Budget b) =>
        new(b.Id, b.Category, b.LimitAmount, b.Month, b.Year, b.CreatedAt);
}
