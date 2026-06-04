using PennyWise.Domain.Entities;

namespace PennyWise.Domain.Interfaces;

/// <summary>
/// Abstraction for JWT token generation and validation.
/// </summary>
public interface ITokenService
{
    string GenerateToken(User user);
}
