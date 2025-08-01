using Microsoft.Extensions.Logging;

namespace Denudey.Application.Services;

using Denudey.Api.Domain.DTOs;
using Denudey.Api.Domain.Entities;
using Denudey.Api.Domain.Events;
using Denudey.Api.Domain.Models;
using Denudey.Api.Services.Cloudinary;
using Denudey.Api.Services.Cloudinary.Interfaces;
using Denudey.Api.Services.Infrastructure.DbContexts;
using Denudey.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

public class EpisodeService
{
    private readonly IEventPublisher _events;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly ILogger<EpisodeService> _logger;
    private readonly IShardRouter _shardRouter;
    private readonly IEpisodeSearchIndexer _episodeSearchIndexer;
    private readonly StatsDbContext _statsDb;

    public EpisodeService(
        IShardRouter router,
        IEventPublisher events,
        ICloudinaryService cloudinaryService,
        IEpisodeSearchIndexer indexer,
        ILogger<EpisodeService> logger,
        StatsDbContext statsDb
    )
    {
        _statsDb = statsDb ?? throw new ArgumentNullException(nameof(statsDb));
        _episodeSearchIndexer = indexer ?? throw new ArgumentNullException(nameof(indexer));
        _shardRouter = router ?? throw new ArgumentNullException(nameof(router));
        _events = events ?? throw new ArgumentNullException(nameof(events));
        _cloudinaryService = cloudinaryService ?? throw new ArgumentNullException(nameof(cloudinaryService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ScamFlixEpisodeDto> CreateEpisodeAsync(Guid userId, string title, string tags, string imageUrl)
    {
        var db = _shardRouter.GetDbForUser(userId);

        var episode = new ScamflixEpisode
        {
            Title = title,
            Tags = tags,
            ImageUrl = imageUrl,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        db.ScamflixEpisodes.Add(episode);
        await db.SaveChangesAsync();

        // Load creator for indexing
        await db.Entry(episode).Reference(e => e.Creator).LoadAsync();

        // Index episode for search - with error handling
        try
        {
            await _episodeSearchIndexer.IndexAsync(episode);
            _logger.LogInformation("Successfully indexed episode {EpisodeId}", episode.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to index episode {EpisodeId} for search", episode.Id);
            // Don't fail the entire operation if search indexing fails
        }

        return new ScamFlixEpisodeDto
        {
            Id = episode.Id,
            Title = episode.Title,
            Tags = episode.Tags,
            ImageUrl = episode.ImageUrl,
            CreatedAt = episode.CreatedAt,
            CreatorId = episode.CreatedBy,
            CreatedBy = episode.Creator?.Username ?? "unknown",
            CreatorAvatarUrl = episode.Creator?.ProfileImageUrl ?? ""
        };
    }

    public async Task<bool> DeleteEpisodeAsync(int episodeId, Guid userId, string role)
    {
        var db = _shardRouter.GetDbForUser(userId);

        var episode = await db.ScamflixEpisodes
            .Include(e => e.Creator)
            .FirstOrDefaultAsync(e => e.Id == episodeId);

        if (episode == null)
        {
            _logger.LogWarning("Episode {EpisodeId} not found", episodeId);
            return false;
        }

        // Check ownership or admin role
        if (episode.Creator.Id != userId && !string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("User {UserId} attempted to delete episode {EpisodeId} without permission", userId, episodeId);
            return false;
        }

        db.ScamflixEpisodes.Remove(episode);
        await db.SaveChangesAsync();

        // Remove from search index - with error handling
        try
        {
            await _episodeSearchIndexer.DeleteAsync(episode.Id);
            _logger.LogInformation("Successfully removed episode {EpisodeId} from search index", episode.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove episode {EpisodeId} from search index", episode.Id);
            // Don't fail the operation if search index removal fails
        }

        return true;
    }

    public async Task<bool> TrackViewAsync(EpisodeActionDto model)
    {
        try
        {
            var view = new EpisodeView
            {
                EpisodeId = model.EpisodeId,
                UserId = model.UserId,
                CreatorId = model.CreatorId,
                CreatorUsername = model.CreatorUsername ?? "",
                CreatorProfileImageUrl = model.CreatorAvatarUrl ?? "",
                CreatedAt = DateTime.UtcNow
            };

            _statsDb.EpisodeViews.Add(view);
            await _statsDb.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track view for episode {EpisodeId}", model.EpisodeId);
            return false;
        }
    }

    public async Task<(bool HasUserLiked, int TotalLikes)> ToggleLikeAsync(EpisodeActionDto model)
    {
        try
        {
            var existing = await _statsDb.EpisodeLikes
                .FirstOrDefaultAsync(l => l.EpisodeId == model.EpisodeId && l.UserId == model.UserId);

            if (existing != null)
            {
                // Unlike
                _statsDb.EpisodeLikes.Remove(existing);
                await _statsDb.SaveChangesAsync();

                var newCount = await _statsDb.EpisodeLikes.CountAsync(l => l.EpisodeId == model.EpisodeId);
                return (false, newCount);
            }
            else
            {
                // Like
                var like = new EpisodeLike
                {
                    EpisodeId = model.EpisodeId,
                    UserId = model.UserId,
                    CreatorId = model.CreatorId,
                    CreatorUsername = model.CreatorUsername ?? "",
                    CreatorProfileImageUrl = model.CreatorAvatarUrl ?? "",
                    CreatedAt = DateTime.UtcNow
                };

                _statsDb.EpisodeLikes.Add(like);
                await _statsDb.SaveChangesAsync();

                var newCount = await _statsDb.EpisodeLikes.CountAsync(l => l.EpisodeId == model.EpisodeId);
                return (true, newCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle like for episode {EpisodeId}", model.EpisodeId);
            throw;
        }
    }
}