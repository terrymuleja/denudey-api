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
using Elastic.Clients.Elasticsearch.Nodes;
using Microsoft.EntityFrameworkCore;

public class EpisodeService 
{
    private IEventPublisher _events;
    private ICloudinaryService _cloudinaryService;
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
        this._statsDb = statsDb;
        this._episodeSearchIndexer = indexer;
        this._shardRouter = router;
        this._events = events ?? throw new ArgumentNullException(nameof(events));
        this._cloudinaryService = cloudinaryService ?? throw new ArgumentNullException(nameof(cloudinaryService));
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

        await db.Entry(episode).Reference(e => e.Creator).LoadAsync();
        await _episodeSearchIndexer.IndexAsync(episode);

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
            .FirstOrDefaultAsync(e => e.Id == episodeId && e.Creator.Id == userId);

        if (episode == null)
            return false;

        db.ScamflixEpisodes.Remove(episode);
        await db.SaveChangesAsync();

        await _episodeSearchIndexer.DeleteAsync(episode.Id);
        return true;
    }

    public async Task<bool> TrackViewAsync(EpisodeActionDto model)
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


    public async Task<(bool HasUserLiked, int TotalLikes)>  ToggleLikeAsync(
    EpisodeActionDto model)
    {
        var existing = await _statsDb.EpisodeLikes
            .FirstOrDefaultAsync(l => l.EpisodeId == model.EpisodeId && l.UserId == model.UserId);

        if (existing != null)
        {
            _statsDb.EpisodeLikes.Remove(existing);
            await _statsDb.SaveChangesAsync();

            var newCount = await _statsDb.EpisodeLikes.CountAsync(l => l.EpisodeId == model.EpisodeId);
            return (false, newCount);
        }
        else
        {
            var like = new EpisodeLike
            {
                EpisodeId = model.EpisodeId,
                UserId = model.UserId,
                CreatorId = model.CreatorId,
                CreatorUsername = model.CreatorUsername?? "",
                CreatorProfileImageUrl = model.CreatorAvatarUrl ?? "",
                CreatedAt = DateTime.UtcNow
            };

            _statsDb.EpisodeLikes.Add(like);
            await _statsDb.SaveChangesAsync();

            var newCount = await _statsDb.EpisodeLikes.CountAsync(l => l.EpisodeId == model.EpisodeId);
            return (true, newCount);
        }
    }

}