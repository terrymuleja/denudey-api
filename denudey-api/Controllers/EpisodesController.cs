using Denudey.Api.Domain;
using Denudey.Api.Domain.Entities;
using Denudey.Api.Models.DTOs;
using Denudey.DataAccess;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Denudey.Api.Controllers;

[ApiController]
[Route("api/episodes")]
public class EpisodesController(ApplicationDbContext db) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEpisodeDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.ImageUrl))
            return BadRequest("Title and image URL are required.");

        var episode = new ScamflixEpisode
        {
            Title = dto.Title,
            Tags = string.Join(",", dto.Tags ?? []),
            ImageUrl = dto.ImageUrl,
            CreatedBy = "anonymous", // Replace with real user ID if needed
            CreatedAt = DateTime.UtcNow
        };

        db.ScamflixEpisodes.Add(episode);
        await db.SaveChangesAsync();

        return Ok(new { episode.Id });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var episodes = await db.ScamflixEpisodes
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

        return Ok(episodes);
    }
}