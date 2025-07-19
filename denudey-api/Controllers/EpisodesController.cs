using System.Security.Claims;
using Denudey.Api.Domain;
using Denudey.Api.Domain.DTOs;
using Denudey.Api.Domain.Entities;
using Denudey.Api.Models;
using Denudey.Api.Models.DTOs;
using Denudey.Api.Services;
using Denudey.DataAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Denudey.Api.Controllers;
[Authorize]
[ApiController]
[Route("api/episodes")]
public class EpisodesController(ApplicationDbContext db, IEpisodesService episodesService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEpisodeDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title) || dto.Title.Length < 5)
            return BadRequest(new { error = "Title must be at least 5 characters long." });

        if (dto.Title.Length > 35)
            return BadRequest(new { error = "Title must be max 35 characters long." });


        if (string.IsNullOrWhiteSpace(dto.ImageUrl))
            return BadRequest(new { error = "Image URL is required." });

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { error = "User ID not found in token." });

        if (string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.ImageUrl))
            return BadRequest("Title and image URL are required.");

        var daId = new Guid(userId);
        var episode = new ScamflixEpisode
        {
            Title = dto.Title,
            Tags = string.Join(",", dto.Tags ?? []),
            ImageUrl = dto.ImageUrl,
            CreatedBy = daId, // Replace with real user ID if needed
            CreatedAt = DateTime.UtcNow
        };

        db.ScamflixEpisodes.Add(episode);
        await db.SaveChangesAsync();

        return Ok(new { episode.Id });
    }

    
    [HttpGet("mine")]
    public async Task<IActionResult> GetMyEpisodes([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(userId, out var id))
            return Unauthorized(new { error = "Invalid user ID." });

        var result = await episodesService.GetEpisodesAsync(id, null, page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// GET /api/episodes?page=1&pageSize=20 → all episodes
    /// GET /api/episodes? search = catfish & page = 2 → search + page 2
    /// </summary>
    /// <param name="search"></param>
    /// <param name="page"></param>
    /// <param name="pageSize"></param>
    /// <returns></returns>
    [HttpGet]
    public async Task<ActionResult<PagedResult<ScamFlixEpisodeDto>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await episodesService.GetEpisodesAsync(null, search, page, pageSize);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteEpisode(Guid id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(userId, out var guid))
            return Unauthorized(new { error = "Invalid user ID." });

        if (string.IsNullOrWhiteSpace(role))
            return Unauthorized(new { error = "Role missing in token." });

        var success = await episodesService.DeleteEpisodeAsync(id, guid, role.ToLower());
        if (!success)
            return Forbid("Not authorized or episode not found.");

        return NoContent();
    }


}