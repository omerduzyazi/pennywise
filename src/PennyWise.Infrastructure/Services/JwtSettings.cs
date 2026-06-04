namespace PennyWise.Infrastructure.Services;

/// <summary>
/// Strongly-typed configuration for JWT token generation.
/// Bound from appsettings.json "JwtSettings" section.
/// </summary>
public class JwtSettings
{
    public const string SectionName = "JwtSettings";

    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpirationInMinutes { get; set; } = 60;
}
