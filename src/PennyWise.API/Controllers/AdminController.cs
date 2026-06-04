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
}
