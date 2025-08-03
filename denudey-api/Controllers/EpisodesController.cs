using System.Security.Claims;
using Denudey.Api.Domain;
using Denudey.Api.Domain.DTOs;
using Denudey.Api.Domain.Entities;
using Denudey.Api.Models;
using Denudey.Api.Models.DTOs;
using Denudey.Api.Services;
using Denudey.Api.Services.Implementations;
using Denudey.Api.Services.Infrastructure;
using Denudey.Api.Services.Infrastructure.DbContexts;
using Denudey.Application.Interfaces;
using Denudey.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Denudey.Api.Controllers;
[Authorize]
[ApiController]
[Route("api/episodes")]
public class EpisodesController(ApplicationDbContext db,
    EpisodeService episodesService,
    EpisodeQueryService episodeQueryService,
    IEpisodeSearchIndexer searchService,
    ILogger<EpisodesController> logger) : DenudeyControlerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEpisodeDto dto)
    {
        // Basic validation can stay in controller for quick fail
        if (string.IsNullOrWhiteSpace(dto.Title) || dto.Title.Length < 5)
            return BadRequest(new { error = "Title must be at least 5 characters long." });

        if (dto.Title.Length > 35)
            return BadRequest(new { error = "Title must be max 35 characters long." });

        if (string.IsNullOrWhiteSpace(dto.ImageUrl))
            return BadRequest(new { error = "Image URL is required." });

        try
        {
            var userId = GetUserId();
           
            // Prepare tags as comma-separated string
            var tags = string.Join(",", dto.Tags ?? []);

            // Delegate to service layer
            var episode = await episodesService.CreateEpisodeAsync(userId, dto.Title, tags, dto.ImageUrl);

            return Ok(episode);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating episode for user {UserId}", GetUserId());
            return StatusCode(500, new { error = "An error occurred while creating the episode." });
        }
    }

    [HttpGet(template: "mine")]
    public async Task<IActionResult> GetMyEpisodes(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var currentUserId = GetUserId();

        var result = await episodeQueryService.GetMyEpisodes(currentUserId, search, page, pageSize);
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
        try
        {
            // Validate parameters
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            var userId = GetUserId();
            var result = await searchService.SearchEpisodesAsync(search, userId, page, pageSize);

            // Always return 200 OK, even if no results
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Unexpected error in GetAll episodes endpoint");

            return StatusCode(500, new
            {
                error = "An unexpected error occurred while retrieving episodes"
            });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteEpisode(int id)
    {
        var userId = GetUserId();
        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        if (string.IsNullOrWhiteSpace(role))
            return Unauthorized(new { error = "Role missing in token." });

        var success = await episodesService.DeleteEpisodeAsync(id, userId, role.ToLower());
        if (!success)
            return Forbid("Not authorized or episode not found.");

        return NoContent();
    }



    [HttpPost(template: "{id}/like")]
    public async Task<IActionResult> ToggleLike(int id, [FromBody] EpisodeActionDto model)
    {
        try
        {
            
            var userId = GetUserId();
            var result = await episodesService.ToggleLikeAsync(model);
            return Ok(new { result.HasUserLiked, result.TotalLikes });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }


    [HttpPost("{id}/view")]
    public async Task<IActionResult> TrackView(int id, [FromBody] EpisodeActionDto model)
    {
        var userId = GetUserId();
        var role = GetUserRole();
        var result = await episodesService.TrackViewAsync(model);
        return result ? Ok() : BadRequest();
    }


}