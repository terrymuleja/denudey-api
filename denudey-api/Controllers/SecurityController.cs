using Denudey.DataAccess;
using DenudeyApi.Models.DTOs;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Denudey.Api.Models.DTOs;
using Denudey.Api.Services.Cloudinary;
using Denudey.Api.Services.Cloudinary.Interfaces;

namespace Denudey.Api.Controllers
{
    [Authorize]
    [Route("api/secure/auth")]
    [ApiController]
    public class SecurityController (ApplicationDbContext db, ICloudinaryService cloudinaryService) : ControllerBase
    {


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




        [HttpGet("me")]
        public async Task<IActionResult> Me(
            [FromServices] ApplicationDbContext db,
            [FromServices] IHttpContextAccessor httpContextAccessor)
        {
            var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? User.FindFirst("sub")?.Value;

            if (id == null) return Unauthorized();

            var userId = Guid.Parse(id);
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
            var role = user.UserRoles
                .Select(ur => ur.Role.Name)
                .FirstOrDefault() ?? "requester"; // Fallback to requester if no roles found
            var response = new MeResponse(
                user.Id,
                user.Username,
                role,
                user.Email,
                user.DeviceId,
                refreshToken?.ExpiresAt,
                user.UserRoles.Select(ur => ur.Role.Name).ToList(),
                CountryCode: user.CountryCode,
                Telephone: user.Phone,
                ProfileImageUrl: user.ProfileImageUrl
            );

            return Ok(response);
        }


        [HttpPut("me")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var user = await db.Users.FindAsync(Guid.Parse(userId));
            if (user == null)
                return NotFound();
            user.CountryCode = dto.CountryCode;
            user.Phone = dto.Phone;
            user.ProfileImageUrl = dto.ProfileImageUrl;

            await db.SaveChangesAsync();
            return Ok();
        }

        [HttpDelete("me/image")]
        public async Task<IActionResult> DeleteProfileImage()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var user = await db.Users.FindAsync(Guid.Parse(userId));
            if (user == null || string.IsNullOrEmpty(user.ProfileImageUrl))
                return NotFound();

            var deleted = await cloudinaryService.DeleteImageFromCloudinary(user.ProfileImageUrl);
            if (deleted)
            {
                user.ProfileImageUrl = null;
                await db.SaveChangesAsync();
                return Ok();
            }

            return BadRequest("Failed to delete image.");
        }


        [HttpGet("test")]
        public IActionResult Test() => Ok("You are authenticated.");
    }

}
