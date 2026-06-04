using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace PennyWise.Tests.Integration;

/// <summary>
/// Integration tests for Budget CRUD and status endpoints.
/// </summary>
public class BudgetEndpointTests : IClassFixture<PennyWiseWebApplicationFactory>
{
    private readonly HttpClient _client;

    public BudgetEndpointTests(PennyWiseWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<string> RegisterAndGetToken()
    {
        var email = $"bg-{Guid.NewGuid():N}@test.com";
        var res = await _client.PostAsJsonAsync("/api/auth/register",
            new { Email = email, Password = "Test123!", FullName = "Budget User" });
        var body = await res.Content.ReadFromJsonAsync<AuthRes>();
        return body!.Token;
    }

    private void SetToken(string token) =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    [Fact]
    public async Task Create_Budget_Returns_Created()
    {
        var token = await RegisterAndGetToken();
        SetToken(token);

        var res = await _client.PostAsJsonAsync("/api/budgets", new
        { Category = "Food", LimitAmount = 2000m, Month = 6, Year = 2026 });

        res.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Duplicate_Budget_Returns_Conflict()
    {
        var token = await RegisterAndGetToken();
        SetToken(token);

        var budget = new { Category = "Transport", LimitAmount = 500m, Month = 6, Year = 2026 };
        await _client.PostAsJsonAsync("/api/budgets", budget);
        var res = await _client.PostAsJsonAsync("/api/budgets", budget);

        res.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Get_Budget_Status_Returns_OK()
    {
        var token = await RegisterAndGetToken();
        SetToken(token);

        await _client.PostAsJsonAsync("/api/budgets", new
        { Category = "Food", LimitAmount = 1000m, Month = 6, Year = 2026 });

        await _client.PostAsJsonAsync("/api/transactions", new
        { Amount = 300m, Description = "Groceries", Category = "Food", Type = 1, TransactionDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc) });

        var res = await _client.GetAsync("/api/budgets/status?month=6&year=2026");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Delete_Budget_Returns_NoContent()
    {
        var token = await RegisterAndGetToken();
        SetToken(token);

        var createRes = await _client.PostAsJsonAsync("/api/budgets", new
        { Category = "Entertainment", LimitAmount = 300m, Month = 6, Year = 2026 });
        var created = await createRes.Content.ReadFromJsonAsync<BudgetRes>();

        var deleteRes = await _client.DeleteAsync($"/api/budgets/{created!.Id}");
        deleteRes.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private record AuthRes(string Token, string Email, string FullName, DateTime ExpiresAt);
    private record BudgetRes(Guid Id, string Category, decimal LimitAmount, int Month, int Year);
}
