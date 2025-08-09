using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Denudey.Api.Services.Infrastructure;
using Denudey.Api.Models.DTOs;
using Denudey.Api.Interfaces;
using DenudeyApi.Models.DTOs;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Denudey.Api.Domain.Entities;
using Denudey.Api.Domain.Models;
using Denudey.Api.Services.Infrastructure.DbContexts;
using Denudey.Application.Interfaces;
using Denudey.Application.Services;
using System.Threading;


namespace Denudey.Api.Controllers;
[AllowAnonymous]
[ApiController]
[Route("api/auth")]
public class PublicAuthController(ApplicationDbContext db,
    ITokenService tokenService,
    IConfiguration configuration,
    IProfileService profileService,
    ILogger<PublicAuthController> logger) : ControllerBase
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
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));
        var user = await db.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid credentials" });

        // Sync user data to social profile in statsDb
        try
        {
            await profileService.SyncUserToSocialProfileAsync(user.Id, cancellationToken);
            logger.LogInformation("Synced user {UserId} to social profile on login", user.Id);
        }
        catch (Exception ex)
        {
            // Log but don't fail login if social sync fails
            logger.LogWarning(ex, "Failed to sync user {UserId} to social profile on login", user.Id);
        }

        var dbRole = user.UserRoles.Select(ur => ur.Role.Name).FirstOrDefault();
        var role = dbRole ?? "requester";

        // 🔒 Revoke all active sessions from *other users* on this device
        var otherUserTokensOnDevice = await db.RefreshTokens
            .Where(t => t.DeviceId == request.DeviceId && t.UserId != user.Id && t.Revoked == null)
            .ToListAsync();

        foreach (var token in otherUserTokensOnDevice)
        {
            token.Revoked = DateTime.UtcNow;
        }

        // 🔒 Revoke all active sessions for this user (on *any* device)
        var userTokens = await db.RefreshTokens
            .Where(t => t.UserId == user.Id && t.Revoked == null)
            .ToListAsync();

        foreach (var token in userTokens)
        {
            token.Revoked = DateTime.UtcNow;
        }

        // 🆕 Create a new refresh token for this login session
        var durationDaysString = configuration["Jwt:RefreshTokenExpiresInDays"];
        if (!double.TryParse(durationDaysString, out double durationDays))
        {
            durationDays = 7.0; // Default to 7 days if parsing fails
        }

        var refreshToken = new RefreshToken
        {
            Token = tokenService.GenerateRefreshToken(),
            UserId = user.Id,
            DeviceId = request.DeviceId,
            ExpiresAt = DateTime.UtcNow.AddDays(durationDays)
        };
        var name = user.Username;
        var email = user.Email;
        db.RefreshTokens.Add(refreshToken);
        await db.SaveChangesAsync();
        var id = user.Id;
        var response = new AuthResponse(
            tokenService.GenerateAccessToken(user.Id.ToString(), role),
            refreshToken.Token,
            role,
            name,
            email,
            id
            
        );

        return Ok(response);
    }


    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshRequest request)
    {
        logger.LogInformation("🔁 Received refresh token: {token}", request.Token);
        logger.LogInformation("🔁 Received device ID: {deviceId}", request.DeviceId);

        var storedToken = await db.RefreshTokens
            .Include(rt => rt.User)
            .ThenInclude(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(rt => rt.Token == request.Token && rt.DeviceId == request.DeviceId);

        if (storedToken == null || storedToken.Revoked != null || storedToken.ExpiresAt < DateTime.UtcNow)
        {
            logger.LogWarning($"Token {request.Token} was expired");
            return Unauthorized(new { message = "Invalid or expired token" });
        }

        logger.LogInformation($"token found: {storedToken.Token}");

        // ✅ Use the execution strategy as the error message suggests
        var strategy = db.Database.CreateExecutionStrategy();

        try
        {
            var result = await strategy.ExecuteAsync(async () =>
            {
                // ✅ Create new token FIRST
                var newToken = new RefreshToken
                {
                    Token = tokenService.GenerateRefreshToken(),
                    UserId = storedToken.UserId,
                    DeviceId = storedToken.DeviceId,
                    ExpiresAt = DateTime.UtcNow.AddDays(7)
                };

                db.RefreshTokens.Add(newToken);
                await db.SaveChangesAsync(); // Save new token first

                // ✅ Only revoke old token AFTER new one is saved
                storedToken.Revoked = DateTime.UtcNow;
                await db.SaveChangesAsync(); // Save revocation

                var role = storedToken.User?.UserRoles?.FirstOrDefault()?.Role.Name ?? "requester";
                logger.LogInformation($"new token: {newToken.Token}");

                return new AuthTokenResponse(
                    tokenService.GenerateAccessToken(storedToken.User!.Id.ToString(), role),
                    newToken.Token
                );
            });

            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError($"Token refresh failed: {ex.Message}");
            return StatusCode(500, new { message = "Token refresh failed" });
        }
    }

    // Require valid token
    [HttpGet("validate")]
    public IActionResult ValidateToken()
    {
        var authHeader = HttpContext.Request.Headers["Authorization"].ToString();

        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            return Unauthorized(new { valid = false, message = "Missing or invalid token format" });

        var token = authHeader.Substring("Bearer ".Length).Trim();

        var tokenHandler = new JwtSecurityTokenHandler();
        var secret = configuration["Jwt:Key"];
        var issuer = configuration["Jwt:Issuer"];
        var audience = configuration["Jwt:Audience"];

        if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience))
        {
            return StatusCode(500, new { message = "Missing JWT configuration" });
        }

        var key = Encoding.UTF8.GetBytes(secret);
        try
        {
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;

            var userId = jwtToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;
            var role = jwtToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Role)?.Value;

            return Ok(new { valid = true, userId, role });
        }
        catch (SecurityTokenExpiredException ste)
        {
            logger.LogWarning($"++++++++++++++++++ Token expired: {ste.Message}");
            return Unauthorized(new { valid = false, message = "Token expired" });
        }
        catch(Exception ex)
        {
            logger.LogWarning($"++++++++++++++++++ Token expired {ex.Message}");
            return Unauthorized(new { valid = false, message = "Token invalid" });
        }
    }


    
   
}