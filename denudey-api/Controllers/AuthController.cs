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

        var existingToken = await db.RefreshTokens
            .FirstOrDefaultAsync(t => t.UserId == user.Id && t.DeviceId == request.DeviceId && !t.Revoked);

        if (existingToken is not null)
        {
            existingToken.Revoked = true;
        }
        else
        {
            // Clean up old tokens for this user/device
            var oldTokens = await db.RefreshTokens
                .Where(t => t.UserId == user.Id && t.DeviceId == request.DeviceId && !t.Revoked)
                .ToListAsync();
            foreach (var token in oldTokens)
            {
                token.Revoked = true;
            }
        }

        // 🆕 Create new refresh token
        var refreshToken = new RefreshToken
        {
            Token = tokenService.GenerateRefreshToken(),
            UserId = user.Id,
            DeviceId = request.DeviceId,
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
    public async Task<IActionResult> RefreshToken([FromBody] RefreshRequest request)
    {
        var storedToken = await db.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == request.Token && rt.DeviceId == request.DeviceId);

        if (storedToken == null || storedToken.Revoked || storedToken.ExpiresAt < DateTime.UtcNow)
            return Unauthorized(new { message = "Invalid or expired token" });

        storedToken.Revoked = true;

        var newToken = new RefreshToken
        {
            Token = tokenService.GenerateRefreshToken(),
            UserId = storedToken.UserId,
            DeviceId = storedToken.DeviceId,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        db.RefreshTokens.Add(newToken);
        await db.SaveChangesAsync();

        return Ok(new AuthResponse(
            tokenService.GenerateAccessToken(storedToken.User!.Id.ToString()),
            newToken.Token
        ));
    }


    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] string refreshToken)
    {
        var token = await db.RefreshTokens.FirstOrDefaultAsync(t => t.Token == refreshToken);

        if (token == null || token.Revoked)
            return BadRequest(new { message = "Already logged out or invalid token" });

        token.Revoked = true;
        await db.SaveChangesAsync();

        return Ok(new { message = "Logged out successfully" });
    }


    [Authorize]
    [HttpGet("test")]
    public IActionResult Test() => Ok("You are authenticated.");
}