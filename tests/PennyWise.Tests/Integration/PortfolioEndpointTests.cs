using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace PennyWise.Tests.Integration;

public class PortfolioEndpointTests : IClassFixture<PennyWiseWebApplicationFactory>
{
    private readonly HttpClient _client;

    public PortfolioEndpointTests(PennyWiseWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<string> RegisterAndGetToken()
    {
        var email = $"pf-{Guid.NewGuid():N}@test.com";
        var res = await _client.PostAsJsonAsync("/api/auth/register",
            new { Email = email, Password = "Test123!", FullName = "PF User" });
        var body = await res.Content.ReadFromJsonAsync<AuthRes>();
        return body!.Token;
    }

    private void SetToken(string token) =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    [Fact]
    public async Task Create_Portfolio_Returns_Created()
    {
        var token = await RegisterAndGetToken();
        SetToken(token);

        var res = await _client.PostAsJsonAsync("/api/portfolios", new { Name = "Long Term" });
        res.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Add_Holding_And_Get_Analytics()
    {
        var token = await RegisterAndGetToken();
        SetToken(token);

        // 1. Create Portfolio
        var pfRes = await _client.PostAsJsonAsync("/api/portfolios", new { Name = "Trading" });
        var pf = await pfRes.Content.ReadFromJsonAsync<PfRes>();

        // 2. Add Holding
        var holdRes = await _client.PostAsJsonAsync($"/api/portfolios/{pf!.Id}/holdings", new
        {
            Symbol = "AAPL", Name = "Apple", InstrumentType = 0, 
            PurchasePrice = 150m, Quantity = 10, PurchaseDate = DateTime.UtcNow
        });
        holdRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var holding = await holdRes.Content.ReadFromJsonAsync<HoldRes>();

        // 3. Update Price to 180
        await _client.PutAsJsonAsync($"/api/holdings/{holding!.Id}/price", new { CurrentPrice = 180m });

        // 4. Get Analytics
        var anRes = await _client.GetFromJsonAsync<AnRes>($"/api/portfolios/{pf.Id}/analytics");
        anRes.Should().NotBeNull();
        anRes!.TotalValue.Should().Be(1800m);
        anRes.AbsoluteReturnAmount.Should().Be(300m); // 1800 - 1500
        anRes.TwrPercentage.Should().Be(20.0); // 20%
    }

    private record AuthRes(string Token);
    private record PfRes(Guid Id, string Name);
    private record HoldRes(Guid Id);
    private record AnRes(decimal TotalValue, decimal AbsoluteReturnAmount, double TwrPercentage);
}
