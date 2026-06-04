using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PennyWise.API.DTOs;
using PennyWise.Domain.Entities;
using PennyWise.Domain.Interfaces;

namespace PennyWise.API.Controllers;

/// <summary>
/// Authentication controller handling user registration, login, and profile retrieval.
/// Passwords are hashed using BCrypt. Authentication is token-based (JWT).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IRepository<User> _userRepository;
    private readonly ITokenService _tokenService;

    public AuthController(IRepository<User> userRepository, ITokenService tokenService)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
    }

    /// <summary>
    /// POST /api/auth/register — Register a new user.
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Check for existing user
        var existingUser = await _userRepository.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (existingUser != null)
        {
            return Conflict(new { Error = "A user with this email already exists." });
        }

        var user = new User
        {
            Email = request.Email.ToLowerInvariant().Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = request.FullName.Trim()
        };

        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        var token = _tokenService.GenerateToken(user);

        return CreatedAtAction(nameof(Me), new AuthResponse(
            Token: token,
            Email: user.Email,
            FullName: user.FullName,
            Role: user.Role,
            ExpiresAt: DateTime.UtcNow.AddMinutes(60)));
    }

    /// <summary>
    /// POST /api/auth/login — Authenticate user and return JWT.
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _userRepository.FirstOrDefaultAsync(
            u => u.Email == request.Email.ToLowerInvariant().Trim());

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { Error = "Invalid email or password." });
        }

        var token = _tokenService.GenerateToken(user);

        return Ok(new AuthResponse(
            Token: token,
            Email: user.Email,
            FullName: user.FullName,
            Role: user.Role,
            ExpiresAt: DateTime.UtcNow.AddMinutes(60)));
    }

    /// <summary>
    /// GET /api/auth/me — Return current authenticated user's profile.
    /// </summary>
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            return Unauthorized();
        }

        var user = await _userRepository.GetByIdAsync(Guid.Parse(userId));
        if (user == null)
        {
            return NotFound(new { Error = "User not found." });
        }

        return Ok(new UserProfileResponse(
            Id: user.Id,
            Email: user.Email,
            FullName: user.FullName,
            Role: user.Role,
            CreatedAt: user.CreatedAt));
    }
}
