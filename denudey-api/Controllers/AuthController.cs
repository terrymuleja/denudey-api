using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Denudey.DataAccess;
using Denudey.DataAccess.Entities;
using Denudey.Api.Models.DTOs;
using Denudey.Api.Interfaces;
using DenudeyApi.Models.DTOs;


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
    [HttpPost("logout-all")]
    public async Task<IActionResult> LogoutAllDevices()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId is null)
            return Unauthorized(new { message = "Invalid access token" });

        var userGuid = Guid.Parse(userId);

        var tokens = await db.RefreshTokens
            .Where(rt => rt.UserId == userGuid && !rt.Revoked)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.Revoked = true;
        }

        await db.SaveChangesAsync();

        return Ok(new { message = "Logged out from all devices" });
    }

    [Authorize]
    [HttpGet("sessions")]
    public async Task<IActionResult> GetSessions()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Unauthorized();

        var userGuid = Guid.Parse(userId);

        var sessions = await db.RefreshTokens
            .Where(rt => rt.UserId == userGuid)
            .OrderByDescending(rt => rt.CreatedAt)
            .Select(rt => new SessionDto(
                rt.DeviceId ?? "Unknown",
                rt.CreatedAt,
                rt.ExpiresAt,
                rt.Revoked
            ))
            .ToListAsync();

        return Ok(sessions);
    }

    [Authorize]
    [HttpDelete("session/{token}")]
    public async Task<IActionResult> RevokeSession(string token)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Unauthorized();

        var tokenToRevoke = await db.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == token && rt.UserId == Guid.Parse(userId));

        if (tokenToRevoke is null)
            return NotFound(new { message = "Session not found" });

        if (tokenToRevoke.Revoked)
            return BadRequest(new { message = "Session already revoked" });

        tokenToRevoke.Revoked = true;
        await db.SaveChangesAsync();

        return Ok(new { message = "Session revoked successfully" });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetProfile([FromHeader(Name = "X-Refresh-Token")] string? refreshToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var userGuid)) return Unauthorized();

        var user = await db.Users
            .Where(u => u.Id == userGuid)
            .Select(u => new { u.Id, u.Username, u.Email })
            .FirstOrDefaultAsync();

        if (user == null) return Unauthorized();

        string? deviceId = null;
        DateTime? expiresAt = null;

        if (!string.IsNullOrEmpty(refreshToken))
        {
            var rt = await db.RefreshTokens
                .FirstOrDefaultAsync(t => t.Token == refreshToken && t.UserId == userGuid);

            if (rt is not null)
            {
                deviceId = rt.DeviceId;
                expiresAt = rt.ExpiresAt;
            }
        }

        return Ok(new MeResponse(user.Id, user.Username, user.Email, deviceId, expiresAt));
    }


    [Authorize]
    [HttpGet("test")]
    public IActionResult Test() => Ok("You are authenticated.");
}