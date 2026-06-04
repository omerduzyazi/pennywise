using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace PennyWise.Tests.Integration;

/// <summary>
/// Integration tests for Auth endpoints: register, login, and profile retrieval.
/// </summary>
public class AuthEndpointTests : IClassFixture<PennyWiseWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthEndpointTests(PennyWiseWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── Register ─────────────────────────────────────────────────

    [Fact]
    public async Task Register_With_Valid_Data_Returns_Created()
    {
        var request = new { Email = "test@pennywise.com", Password = "Test123!", FullName = "Test User" };
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body.Should().NotBeNull();
        body!.Token.Should().NotBeNullOrEmpty();
        body.Email.Should().Be("test@pennywise.com");
    }

    [Fact]
    public async Task Register_With_Missing_Fields_Returns_BadRequest()
    {
        var request = new { Email = "", Password = "", FullName = "" };
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_Duplicate_Email_Returns_Conflict()
    {
        var request = new { Email = "duplicate@pennywise.com", Password = "Test123!", FullName = "User A" };

        await _client.PostAsJsonAsync("/api/auth/register", request);
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ── Login ────────────────────────────────────────────────────

    [Fact]
    public async Task Login_With_Valid_Credentials_Returns_OK()
    {
        var registerReq = new { Email = "login-test@pennywise.com", Password = "Test123!", FullName = "Login User" };
        await _client.PostAsJsonAsync("/api/auth/register", registerReq);

        var loginReq = new { Email = "login-test@pennywise.com", Password = "Test123!" };
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginReq);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body.Should().NotBeNull();
        body!.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_With_Wrong_Password_Returns_Unauthorized()
    {
        var registerReq = new { Email = "wrongpw@pennywise.com", Password = "CorrectPw!", FullName = "Wrong PW User" };
        await _client.PostAsJsonAsync("/api/auth/register", registerReq);

        var loginReq = new { Email = "wrongpw@pennywise.com", Password = "WrongPw!" };
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginReq);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_With_Nonexistent_User_Returns_Unauthorized()
    {
        var loginReq = new { Email = "nobody@pennywise.com", Password = "Test123!" };
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginReq);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Me (Profile) ─────────────────────────────────────────────

    [Fact]
    public async Task Me_Without_Token_Returns_Unauthorized()
    {
        var response = await _client.GetAsync("/api/auth/me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_With_Valid_Token_Returns_Profile()
    {
        // Register
        var registerReq = new { Email = "profile@pennywise.com", Password = "Test123!", FullName = "Profile User" };
        var regResponse = await _client.PostAsJsonAsync("/api/auth/register", registerReq);
        var authBody = await regResponse.Content.ReadFromJsonAsync<AuthResponse>();

        // Call /me with token
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", authBody!.Token);

        var response = await _client.GetAsync("/api/auth/me");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var profile = await response.Content.ReadFromJsonAsync<ProfileResponse>();
        profile.Should().NotBeNull();
        profile!.Email.Should().Be("profile@pennywise.com");
        profile.FullName.Should().Be("Profile User");

        // Reset default header
        _client.DefaultRequestHeaders.Authorization = null;
    }

    // ── Response Records ─────────────────────────────────────────

    private record AuthResponse(string Token, string Email, string FullName, DateTime ExpiresAt);
    private record ProfileResponse(Guid Id, string Email, string FullName, DateTime CreatedAt);
}
