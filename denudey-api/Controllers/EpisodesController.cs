using System.Security.Claims;
using Denudey.Api.Domain;
using Denudey.Api.Domain.Entities;
using Denudey.Api.Models.DTOs;
using Denudey.DataAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Denudey.Api.Controllers;
[Authorize]
[ApiController]
[Route("api/episodes")]
public class EpisodesController(ApplicationDbContext db) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEpisodeDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title) || dto.Title.Length < 20)
            return BadRequest(new { error = "Title must be at least 20 characters long." });

        if (string.IsNullOrWhiteSpace(dto.ImageUrl))
            return BadRequest(new { error = "Image URL is required." });

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { error = "User ID not found in token." });

        if (string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.ImageUrl))
            return BadRequest("Title and image URL are required.");

        var episode = new ScamflixEpisode
        {
            Title = dto.Title,
            Tags = string.Join(",", dto.Tags ?? []),
            ImageUrl = dto.ImageUrl,
            CreatedBy = userId ?? "anonymous", // Replace with real user ID if needed
            CreatedAt = DateTime.UtcNow
        };

        db.ScamflixEpisodes.Add(episode);
        await db.SaveChangesAsync();

        return Ok(new { episode.Id });
    }

    
    [HttpGet("mine")]
    public async Task<IActionResult> GetMyEpisodes()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { error = "User ID not found in token." });

        var episodes = await db.ScamflixEpisodes
            .Where(e => e.CreatedBy == userId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

        return Ok(episodes);
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
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0 || pageSize > 100) pageSize = 10;

        var query = db.ScamflixEpisodes.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            string keyword = search.ToLower();
            query = query.Where(e =>
                e.Title.ToLower().Contains(keyword) ||
                e.Tags.ToLower().Contains(keyword)
            );
        }

        var totalItems = await query.CountAsync();

        var episodes = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new
        {
            totalItems,
            page,
            pageSize,
            items = episodes
        });
    }

}