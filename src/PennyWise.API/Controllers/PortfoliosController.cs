using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PennyWise.API.DTOs;
using PennyWise.Domain.Entities;
using PennyWise.Domain.Interfaces;

namespace PennyWise.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PortfoliosController : ControllerBase
{
    private readonly IRepository<Portfolio> _portfolioRepo;
    private readonly IRepository<Holding> _holdingRepo;
    private readonly IPortfolioAnalyticsService _analyticsService;

    public PortfoliosController(
        IRepository<Portfolio> portfolioRepo,
        IRepository<Holding> holdingRepo,
        IPortfolioAnalyticsService analyticsService)
    {
        _portfolioRepo = portfolioRepo;
        _holdingRepo = holdingRepo;
        _analyticsService = analyticsService;
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    // ── Portfolios ──────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = GetUserId();
        var portfolios = await _portfolioRepo.FindAsync(p => p.UserId == userId);
        var responses = portfolios.Select(p => new PortfolioResponse(p.Id, p.Name, p.CreatedAt));
        return Ok(responses);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePortfolioRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { Error = "Portfolio name is required." });

        var portfolio = new Portfolio
        {
            Name = request.Name.Trim(),
            UserId = GetUserId()
        };

        await _portfolioRepo.AddAsync(portfolio);
        await _portfolioRepo.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), new PortfolioResponse(portfolio.Id, portfolio.Name, portfolio.CreatedAt));
    }

    [HttpGet("{id:guid}/analytics")]
    public async Task<IActionResult> GetAnalytics(Guid id)
    {
        var userId = GetUserId();
        var portfolio = await _portfolioRepo.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);
        if (portfolio == null) return NotFound(new { Error = "Portfolio not found." });

        var holdings = await _holdingRepo.FindAsync(h => h.PortfolioId == id);
        
        var (totalValue, absReturn, twr) = _analyticsService.CalculateReturns(holdings);

        return Ok(new PortfolioAnalyticsResponse(
            portfolio.Id,
            portfolio.Name,
            totalValue,
            absReturn,
            twr));
    }

    // ── Holdings ────────────────────────────────────────────────

    [HttpGet("{id:guid}/holdings")]
    public async Task<IActionResult> GetHoldings(Guid id)
    {
        var userId = GetUserId();
        var portfolio = await _portfolioRepo.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);
        if (portfolio == null) return NotFound(new { Error = "Portfolio not found." });

        var holdings = await _holdingRepo.FindAsync(h => h.PortfolioId == id);
        var responses = holdings.Select(MapToResponse);
        return Ok(responses);
    }

    [HttpPost("{id:guid}/holdings")]
    public async Task<IActionResult> AddHolding(Guid id, [FromBody] CreateHoldingRequest request)
    {
        var userId = GetUserId();
        var portfolio = await _portfolioRepo.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);
        if (portfolio == null) return NotFound(new { Error = "Portfolio not found." });

        if (request.PurchasePrice < 0 || request.Quantity <= 0)
            return BadRequest(new { Error = "Invalid price or quantity." });

        var holding = new Holding
        {
            TickerSymbol = request.Symbol.Trim().ToUpper(),
            InstrumentName = request.Name.Trim(),
            InstrumentType = request.InstrumentType,
            PurchasePrice = request.PurchasePrice,
            CurrentPrice = request.PurchasePrice, // Initially, current price = purchase price
            Quantity = request.Quantity,
            PurchaseDate = request.PurchaseDate.ToUniversalTime(),
            PortfolioId = portfolio.Id
        };

        await _holdingRepo.AddAsync(holding);
        await _holdingRepo.SaveChangesAsync();

        return CreatedAtAction(nameof(GetHoldings), new { id = portfolio.Id }, MapToResponse(holding));
    }

    [HttpPut("/api/holdings/{holdingId:guid}/price")]
    public async Task<IActionResult> UpdateHoldingPrice(Guid holdingId, [FromBody] UpdateHoldingPriceRequest request)
    {
        var userId = GetUserId();
        
        // Ensure user owns the portfolio this holding belongs to
        var holding = await _holdingRepo.FirstOrDefaultAsync(h => h.Id == holdingId);
        if (holding == null) return NotFound(new { Error = "Holding not found." });

        var portfolio = await _portfolioRepo.FirstOrDefaultAsync(p => p.Id == holding.PortfolioId && p.UserId == userId);
        if (portfolio == null) return NotFound(new { Error = "Holding not found." }); // Hide ownership details

        if (request.CurrentPrice < 0)
            return BadRequest(new { Error = "Price cannot be negative." });

        holding.CurrentPrice = request.CurrentPrice;
        
        _holdingRepo.Update(holding);
        await _holdingRepo.SaveChangesAsync();

        return Ok(MapToResponse(holding));
    }

    [HttpDelete("/api/holdings/{holdingId:guid}")]
    public async Task<IActionResult> DeleteHolding(Guid holdingId)
    {
        var userId = GetUserId();
        
        var holding = await _holdingRepo.FirstOrDefaultAsync(h => h.Id == holdingId);
        if (holding == null) return NotFound(new { Error = "Holding not found." });

        var portfolio = await _portfolioRepo.FirstOrDefaultAsync(p => p.Id == holding.PortfolioId && p.UserId == userId);
        if (portfolio == null) return NotFound(new { Error = "Holding not found." });

        _holdingRepo.Remove(holding);
        await _holdingRepo.SaveChangesAsync();

        return NoContent();
    }

    private static HoldingResponse MapToResponse(Holding h) =>
        new(h.Id, h.TickerSymbol, h.InstrumentName, h.InstrumentType, h.PurchasePrice, h.CurrentPrice, h.Quantity, h.PurchaseDate);
}
