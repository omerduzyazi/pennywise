using Microsoft.AspNetCore.Mvc;

namespace PennyWise.API.Controllers;

/// <summary>
/// Health check endpoint to verify API availability.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// Returns the API health status and server timestamp.
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            Status = "Healthy",
            Service = "PennyWise API",
            Version = "1.0.0",
            Timestamp = DateTime.UtcNow
        });
    }
}
