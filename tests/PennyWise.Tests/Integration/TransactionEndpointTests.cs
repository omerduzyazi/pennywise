using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace PennyWise.Tests.Integration;

/// <summary>
/// Integration tests for Transaction CRUD endpoints.
/// </summary>
public class TransactionEndpointTests : IClassFixture<PennyWiseWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TransactionEndpointTests(PennyWiseWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<string> RegisterAndGetToken()
    {
        var email = $"tx-{Guid.NewGuid():N}@test.com";
        var res = await _client.PostAsJsonAsync("/api/auth/register",
            new { Email = email, Password = "Test123!", FullName = "TX User" });
        var body = await res.Content.ReadFromJsonAsync<AuthRes>();
        return body!.Token;
    }

    private void SetToken(string token) =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    [Fact]
    public async Task Create_Transaction_Returns_Created()
    {
        var token = await RegisterAndGetToken();
        SetToken(token);

        var res = await _client.PostAsJsonAsync("/api/transactions", new
        {
            Amount = 1500.00m,
            Description = "Salary",
            Category = "Income",
            Type = 0,
            TransactionDate = DateTime.UtcNow
        });

        res.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Get_Transactions_Returns_OK()
    {
        var token = await RegisterAndGetToken();
        SetToken(token);

        await _client.PostAsJsonAsync("/api/transactions", new
        {
            Amount = 50m, Description = "Coffee", Category = "Food",
            Type = 1, TransactionDate = DateTime.UtcNow
        });

        var res = await _client.GetAsync("/api/transactions");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_Summary_Returns_Correct_Totals()
    {
        var token = await RegisterAndGetToken();
        SetToken(token);

        await _client.PostAsJsonAsync("/api/transactions", new
        { Amount = 1000m, Description = "Salary", Category = "Work", Type = 0, TransactionDate = DateTime.UtcNow });
        await _client.PostAsJsonAsync("/api/transactions", new
        { Amount = 200m, Description = "Groceries", Category = "Food", Type = 1, TransactionDate = DateTime.UtcNow });

        var res = await _client.GetFromJsonAsync<SummaryRes>("/api/transactions/summary");
        res.Should().NotBeNull();
        res!.TotalIncome.Should().Be(1000m);
        res.TotalExpenses.Should().Be(200m);
        res.NetBalance.Should().Be(800m);
    }

    [Fact]
    public async Task Delete_Transaction_Returns_NoContent()
    {
        var token = await RegisterAndGetToken();
        SetToken(token);

        var createRes = await _client.PostAsJsonAsync("/api/transactions", new
        { Amount = 10m, Description = "Test", Category = "Misc", Type = 1, TransactionDate = DateTime.UtcNow });
        var created = await createRes.Content.ReadFromJsonAsync<TxRes>();

        var deleteRes = await _client.DeleteAsync($"/api/transactions/{created!.Id}");
        deleteRes.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Unauthenticated_Request_Returns_Unauthorized()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var res = await _client.GetAsync("/api/transactions");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private record AuthRes(string Token, string Email, string FullName, DateTime ExpiresAt);
    private record TxRes(Guid Id, decimal Amount, string Description, string Category, int Type);
    private record SummaryRes(decimal TotalIncome, decimal TotalExpenses, decimal NetBalance, int TransactionCount);
}
