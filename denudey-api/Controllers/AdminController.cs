using Denudey.Api.Services.Infrastructure.DbContexts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DenudeyApi.Models.DTOs;
using Denudey.Api.Services.Infrastructure;

namespace DenudeyApi.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet("sessions")]
    public async Task<IActionResult> GetAllSessions()
    {
        var sessions = await db.RefreshTokens
            .Include(rt => rt.User)
            .OrderByDescending(rt => rt.CreatedAt)
            .Select(rt => new
            {
                UserId = rt.User.Id,
                Username = rt.User.Username,
                Email = rt.User.Email,
                DeviceId = rt.DeviceId ?? "Unknown",
                CreatedAt = rt.CreatedAt,
                ExpiresAt = rt.ExpiresAt,
                Revoked = rt.Revoked
            })
            .ToListAsync();

        return Ok(sessions);
    }

    [HttpPost("sessions/revoke")]
    public async Task<IActionResult> RevokeSession([FromBody] RevokeTokenRequest request)
    {
        var token = await db.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == request.Token);

        if (token == null)
            return NotFound("Session not found");

        if (token.Revoked != null)
            return BadRequest("Session is already revoked");

        token.Revoked = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Ok("Session revoked successfully");
    }

}