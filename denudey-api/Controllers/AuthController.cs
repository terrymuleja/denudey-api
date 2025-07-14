using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Denudey.Api.Interfaces;
using Denudey.Api.Models.DTOs;


namespace Denudey.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(ITokenService tokenService) : ControllerBase
{
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        // Simulate user check – replace with DB lookup
        if (request.Username == "demo" && request.Password == "123")
        {
            var accessToken = tokenService.GenerateAccessToken("user-id-123");
            var refreshToken = tokenService.GenerateRefreshToken();

            var response = new AuthResponse(accessToken, refreshToken);
            return Ok(response);
        }

        return Unauthorized(new { message = "Invalid credentials" });
    }

    [Authorize]
    [HttpGet("test")]
    public IActionResult Test() => Ok("You're authenticated!");
}
