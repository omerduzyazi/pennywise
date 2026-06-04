using PennyWise.Domain.Entities;
using PennyWise.Domain.Interfaces;

namespace PennyWise.Infrastructure.Services;

public class PortfolioAnalyticsService : IPortfolioAnalyticsService
{
    public (decimal TotalValue, decimal AbsoluteReturnAmount, double TwrPercentage) CalculateReturns(IEnumerable<Holding> holdings)
    {
        if (holdings == null || !holdings.Any())
            return (0m, 0m, 0.0);

        decimal totalValue = 0m;
        decimal totalCostBasis = 0m;

        foreach (var holding in holdings)
        {
            totalValue += holding.CurrentPrice * holding.Quantity;
            totalCostBasis += holding.PurchasePrice * holding.Quantity;
        }

        decimal absoluteReturnAmount = totalValue - totalCostBasis;

        // Approximation of TWR without mid-period cashflows.
        // TWR = (Ending Value / Beginning Value) - 1
        double twrPercentage = totalCostBasis > 0 
            ? (double)((totalValue / totalCostBasis) - 1) * 100 
            : 0;

        return (totalValue, absoluteReturnAmount, Math.Round(twrPercentage, 2));
    }
}
