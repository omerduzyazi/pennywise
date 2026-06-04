using FluentAssertions;
using Microsoft.Extensions.Options;
using PennyWise.Domain.Entities;
using PennyWise.Infrastructure.Services;
using Xunit;

namespace PennyWise.Tests.Unit;

/// <summary>
/// Unit tests for the TokenService JWT generation.
/// </summary>
public class TokenServiceTests
{
    private readonly TokenService _tokenService;

    public TokenServiceTests()
    {
        var jwtSettings = Options.Create(new JwtSettings
        {
            Secret = "PennyW1se-Sup3r-S3cur3-K3y-2026-Th1s-Must-Be-At-Least-32-Chars!",
            Issuer = "PennyWise.API",
            Audience = "PennyWise.Web",
            ExpirationInMinutes = 60
        });

        _tokenService = new TokenService(jwtSettings);
    }

    [Fact]
    public void GenerateToken_Returns_NonEmpty_String()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@pennywise.com",
            FullName = "Test User"
        };

        var token = _tokenService.GenerateToken(user);

        token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateToken_Returns_Valid_JWT_Format()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "jwt@pennywise.com",
            FullName = "JWT User"
        };

        var token = _tokenService.GenerateToken(user);

        // JWT has three parts separated by dots
        token.Split('.').Should().HaveCount(3);
    }

    [Fact]
    public void GenerateToken_Different_Users_Return_Different_Tokens()
    {
        var user1 = new User { Id = Guid.NewGuid(), Email = "user1@pw.com", FullName = "User 1" };
        var user2 = new User { Id = Guid.NewGuid(), Email = "user2@pw.com", FullName = "User 2" };

        var token1 = _tokenService.GenerateToken(user1);
        var token2 = _tokenService.GenerateToken(user2);

        token1.Should().NotBe(token2);
    }
}
