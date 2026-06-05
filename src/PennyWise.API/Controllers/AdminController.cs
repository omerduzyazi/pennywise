using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PennyWise.Domain.Entities;
using PennyWise.Domain.Interfaces;

namespace PennyWise.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IRepository<User> _userRepository;

    public AdminController(IRepository<User> userRepository)
    {
        _userRepository = userRepository;
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _userRepository.GetAllAsync();
        var userDtos = users.Select(u => new
        {
            u.Id,
            u.Email,
            u.FullName,
            u.Role,
            u.CreatedAt
        });

        return Ok(userDtos);
    }

    [HttpDelete("users/{id}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) return NotFound("Kullanıcı bulunamadı.");

        if (user.Role == "Admin") return BadRequest("Admin yetkisine sahip bir kullanıcıyı silemezsiniz.");

        _userRepository.Remove(user);
        await _userRepository.SaveChangesAsync();

        return NoContent();
    }
}
