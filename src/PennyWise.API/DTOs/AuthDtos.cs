using System.ComponentModel.DataAnnotations;

namespace PennyWise.API.DTOs;

/// <summary>
/// Data Transfer Objects for authentication endpoints.
/// </summary>
public record RegisterRequest(
    [Required, EmailAddress] string Email, 
    [Required, MinLength(6)] string Password, 
    [Required, StringLength(100)] string FullName);
    
public record LoginRequest(
    [Required, EmailAddress] string Email, 
    [Required] string Password);
    
public record AuthResponse(string Token, string Email, string FullName, string Role, DateTime ExpiresAt);
public record UserProfileResponse(Guid Id, string Email, string FullName, string Role, DateTime CreatedAt);
