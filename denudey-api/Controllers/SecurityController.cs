using Denudey.Api.Services.Infrastructure;
using DenudeyApi.Models.DTOs;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Denudey.Api.Models.DTOs;
using Denudey.Api.Services.Cloudinary;
using Denudey.Api.Services.Cloudinary.Interfaces;
using Denudey.Api.Domain.Entities;
using Denudey.Api.Services.Infrastructure.DbContexts;
using Denudey.Api.Domain.DTOs;
using Denudey.Application.Interfaces;
using Elastic.Clients.Elasticsearch.Security;
using System.Threading;

namespace Denudey.Api.Controllers
{
    [Authorize]
    [Route("api/secure/auth")]
    [ApiController]
    public class SecurityController (ApplicationDbContext db, ICloudinaryService cloudinaryService, IProfileService profileService) : ControllerBase
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
            var response = await profileService.GetUserProfileAsync(userId, accessToken);
            
            return Ok(response);
        }


        [HttpPut("me")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto, CancellationToken cancellationToken)
        {
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            if (string.IsNullOrEmpty(id))
                return Unauthorized();
       
            var userId = Guid.Parse(id);

            await profileService.UpdateProfileAsync(userId, dto, cancellationToken);
            
            return Ok();
        }

        [HttpDelete("me/image")]
        public async Task<IActionResult> DeleteProfileImage()
        {
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(id))
                return Unauthorized();

            var user = await db.Users.FindAsync(Guid.Parse(id));
            if (user == null || string.IsNullOrEmpty(user.ProfileImageUrl))
                return NotFound();

            var userId = Guid.Parse(id);
            var deleted = await profileService.DeleteProfileImageAsync(userId);
       
            if (deleted)
            {
                return Ok();
            }

            return BadRequest("Failed to delete image.");
        }


        [HttpPut("profile/privacy")]
        public async Task<IActionResult> UpdatePrivacy([FromBody] UpdatePrivacyDto dto, CancellationToken cancellationToken)
        {
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(id))
                return Unauthorized();
            if (string.IsNullOrEmpty(id)) return Unauthorized();

            var userId = Guid.Parse(id);
            await profileService.UpdatePrivacyAsync(userId, dto.IsPrivate, cancellationToken);
            
            return Ok();
        }

        [HttpPut("profile/role")]
        public async Task<IActionResult> UpdateRole([FromBody] UpdateRoleDto dto, CancellationToken cancellationToken)
        {
            var normalizedRole = dto.Role?.ToLower();
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(id))
                return Unauthorized();
            if (string.IsNullOrEmpty(id)) return Unauthorized();

            if (normalizedRole != "model" && normalizedRole != "requester")
                return BadRequest(new { error = "Role must be 'Model' or 'Requester'." });

            var userId = Guid.Parse(id);

            await profileService.UpdateRoleAsync(userId, normalizedRole, cancellationToken);

            return Ok();
        }

    }

}
