namespace PennyWise.Domain.Interfaces;

public interface IPortfolioAnalyticsService
{
    /// <summary>
    /// Calculates portfolio analytics based on current holdings.
    /// Simplified TWR: (Sum of Current Value) / (Sum of Purchase Value) - 1.
    /// In a real-world scenario with cash flows, this would calculate daily sub-period returns.
    /// </summary>
    (decimal TotalValue, decimal AbsoluteReturnAmount, double TwrPercentage) CalculateReturns(IEnumerable<Entities.Holding> holdings);
}
