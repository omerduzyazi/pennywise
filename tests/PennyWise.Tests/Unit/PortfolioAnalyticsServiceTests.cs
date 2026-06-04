using FluentAssertions;
using PennyWise.Domain.Entities;
using PennyWise.Infrastructure.Services;
using Xunit;

namespace PennyWise.Tests.Unit;

public class PortfolioAnalyticsServiceTests
{
    private readonly PortfolioAnalyticsService _sut;

    public PortfolioAnalyticsServiceTests()
    {
        _sut = new PortfolioAnalyticsService();
    }

    [Fact]
    public void CalculateReturns_EmptyHoldings_ReturnsZeros()
    {
        var result = _sut.CalculateReturns(new List<Holding>());

        result.TotalValue.Should().Be(0m);
        result.AbsoluteReturnAmount.Should().Be(0m);
        result.TwrPercentage.Should().Be(0.0);
    }

    [Fact]
    public void CalculateReturns_WithGains_CalculatesCorrectly()
    {
        var holdings = new List<Holding>
        {
            new Holding { Quantity = 10, PurchasePrice = 100m, CurrentPrice = 150m }, // Cost: 1000, Value: 1500 (50% gain)
            new Holding { Quantity = 5, PurchasePrice = 200m, CurrentPrice = 220m }   // Cost: 1000, Value: 1100 (10% gain)
        };
        // Total Cost: 2000
        // Total Value: 2600
        // Abs Return: 600
        // TWR: (2600 / 2000) - 1 = 0.3 -> 30%

        var result = _sut.CalculateReturns(holdings);

        result.TotalValue.Should().Be(2600m);
        result.AbsoluteReturnAmount.Should().Be(600m);
        result.TwrPercentage.Should().Be(30.0);
    }

    [Fact]
    public void CalculateReturns_WithLoss_CalculatesCorrectly()
    {
        var holdings = new List<Holding>
        {
            new Holding { Quantity = 100, PurchasePrice = 50m, CurrentPrice = 40m } // Cost: 5000, Value: 4000
        };

        var result = _sut.CalculateReturns(holdings);

        result.TotalValue.Should().Be(4000m);
        result.AbsoluteReturnAmount.Should().Be(-1000m);
        result.TwrPercentage.Should().Be(-20.0);
    }
}
