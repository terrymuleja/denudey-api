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
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, [FromServices] ApplicationDbContext db)
    {
        if (await db.Users.AnyAsync(u => u.Email == request.Email))
            return Conflict("Email already in use");


        // Device already used
        if (await db.Users.AnyAsync(u => u.DeviceId == request.DeviceId))
            return Conflict(new { error = "This device has already been used to register an account" });

        var user = new ApplicationUser
        {
            Username = request.Email,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            DeviceId = request.DeviceId,
            CreatedAt = DateTime.UtcNow
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(); // Save user first to get the ID
        var aRole = request.Role?.ToLowerInvariant() == "model" ? RoleNames.Model : RoleNames.Requester;
        // Assign default role: "model"
        var role = await db.Roles.FirstOrDefaultAsync(r => r.Name == aRole);
        if (role is null) return Problem("Selected role not found");

        db.UserRoles.Add(new UserRole
        {
            UserId = user.Id,
            RoleId = role.Id
        });

        await db.SaveChangesAsync();

        return Ok("Registered successfully");
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid credentials" });

        var existingToken = await db.RefreshTokens
            .FirstOrDefaultAsync(t => t.UserId == user.Id && t.DeviceId == request.DeviceId && t.Revoked == null);
        var dbRole = user.UserRoles
            .Select(ur => ur.Role.Name)
            .FirstOrDefault(); // should never be null if role is properly assigned

        var role = dbRole ?? "requester"; // Default to "requester" if no role found    
        if (existingToken is not null)
        {
            existingToken.Revoked = DateTime.UtcNow;
        }
        else
        {
            // Clean up old tokens for this user/device
            var oldTokens = await db.RefreshTokens
                .Where(t => t.UserId == user.Id && t.DeviceId == request.DeviceId && t.Revoked == null)
                .ToListAsync();
            foreach (var token in oldTokens)
            {
                token.Revoked = DateTime.UtcNow;
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
            refreshToken.Token,
            role
        );
        return Ok(response);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshRequest request)
    {
        var storedToken = await db.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == request.Token && rt.DeviceId == request.DeviceId);

        if (storedToken == null || storedToken.Revoked != null || storedToken.ExpiresAt < DateTime.UtcNow)
            return Unauthorized(new { message = "Invalid or expired token" });

        storedToken.Revoked = DateTime.UtcNow;

        var newToken = new RefreshToken
        {
            Token = tokenService.GenerateRefreshToken(),
            UserId = storedToken.UserId,
            DeviceId = storedToken.DeviceId,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        db.RefreshTokens.Add(newToken);
        await db.SaveChangesAsync();

        return Ok(new AuthTokenResponse(
            tokenService.GenerateAccessToken(storedToken.User!.Id.ToString()),
            newToken.Token
        ));
    }


    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] string refreshToken)
    {
        var token = await db.RefreshTokens.FirstOrDefaultAsync(t => t.Token == refreshToken);

        if (token == null || token.Revoked != null)
            return BadRequest(new { message = "Already logged out or invalid token" });

        token.Revoked = DateTime.UtcNow;
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
            .Where(rt => rt.UserId == userGuid && rt.Revoked == null)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.Revoked = DateTime.UtcNow;
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

        if (tokenToRevoke.Revoked == null)
            return BadRequest(new { message = "Session already revoked" });

        tokenToRevoke.Revoked = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Ok(new { message = "Session revoked successfully" });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me(
        [FromServices] ApplicationDbContext db,
        [FromServices] IHttpContextAccessor httpContextAccessor)
    {
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "uid");
        if (userIdClaim == null) return Unauthorized();

        var userId = Guid.Parse(userIdClaim.Value);

        var user = await db.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null) return NotFound("User not found");

        // Extract current token from Authorization header
        var accessToken = httpContextAccessor.HttpContext?.Request.Headers["Authorization"]
            .FirstOrDefault()?.Replace("Bearer ", "");

        var refreshToken = await db.RefreshTokens
            .Where(rt => rt.UserId == user.Id && rt.Token == accessToken)
            .OrderByDescending(rt => rt.ExpiresAt)
            .FirstOrDefaultAsync();

        var response = new MeResponse(
            user.Id,
            user.Username,
            user.Email,
            user.DeviceId,
            refreshToken?.ExpiresAt,
            user.UserRoles.Select(ur => ur.Role.Name).ToList()
        );

        return Ok(response);
    }


    [Authorize]
    [HttpGet("test")]
    public IActionResult Test() => Ok("You are authenticated.");
}