using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Denudey.DataAccess;
using Denudey.DataAccess.Entities;
using Denudey.Api.Models.DTOs;
using Denudey.Api.Interfaces;

namespace denudey_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(ApplicationDbContext db, ITokenService tokenService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var exists = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (exists != null)
            return BadRequest(new { message = "Email already registered" });

        var user = new ApplicationUser
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            DeviceId = request.DeviceId
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return Ok(new { message = "User registered successfully" });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid credentials" });

        var refreshToken = new RefreshToken
        {
            Token = tokenService.GenerateRefreshToken(),
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        db.RefreshTokens.Add(refreshToken);
        await db.SaveChangesAsync();

        var response = new AuthResponse(
            tokenService.GenerateAccessToken(user.Id.ToString()),
            refreshToken.Token
        );
        return Ok(response);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] string refreshToken)
    {
        var storedToken = await db.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        if (storedToken == null || storedToken.ExpiresAt < DateTime.UtcNow)
            return Unauthorized(new { message = "Invalid or expired refresh token" });

        var newAccessToken = tokenService.GenerateAccessToken(storedToken.User!.Id.ToString());
        return Ok(new AuthResponse(newAccessToken, refreshToken));
    }


    [Authorize]
    [HttpGet("test")]
    public IActionResult Test() => Ok("You are authenticated.");
}