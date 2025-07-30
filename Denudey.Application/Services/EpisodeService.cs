using Microsoft.Extensions.Logging;

namespace Denudey.Application.Services;
using Denudey.Api.Domain.Entities;



using Denudey.Api.Domain.Events;
using Denudey.Api.Domain.Models;
using Denudey.Api.Services.Cloudinary;
using Denudey.Api.Services.Cloudinary.Interfaces;
using Denudey.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

public class EpisodeService : EpisodeServiceBase
{
    private IEventPublisher _events;
    private ICloudinaryService _cloudinaryService;
    private readonly ILogger<EpisodeService> _logger;
    public EpisodeService(
        IShardRouter router,
        IEventPublisher events,
        ICloudinaryService cloudinaryService,
        ILogger<EpisodeService> logger
    ) : base(router)
    {

        this._events = events ?? throw new ArgumentNullException(nameof(events));
        this._cloudinaryService = cloudinaryService ?? throw new ArgumentNullException(nameof(cloudinaryService));
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    public async Task<(bool HasUserLiked, int Likes)> ToggleLikeAsync(Guid userId, int episodeId)
    {
        var db = shardRouter.GetDbForUser(userId);
        Guid? creatorId = await GetCreatorIdAsync(episodeId, userId);

        if (episodeId <= 0)
            throw new ArgumentException("Episode ID must be greater than zero.", nameof(episodeId));

        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));

        try
        {
            if (creatorId != null)
            {
                var strategy = db.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () =>
                {
                    
                });


                
                return (true, 0);
            }
            
        }
        catch (DbUpdateException ex)
        {
            if (ex.InnerException?.Message?.Contains("FOREIGN KEY constraint") == true)
                throw new InvalidOperationException("Invalid episode or user reference.", ex);

            throw new InvalidOperationException("An error occurred while updating the like status.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while toggling like for episode {EpisodeId} by user {UserId}.", episodeId, userId);
            throw new InvalidOperationException("An unexpected error occurred while processing the like toggle.", ex);
        }
        return (false, 0);
    }

    public async Task<bool> TrackViewEpisodeAsync(Guid userId, int episodeId, string role)
    {
        Guid? creatorId = await GetCreatorIdAsync(episodeId, userId);

        if (creatorId != null)
        {
            var db = shardRouter.GetDbForUser(userId);
            try
            {
                var strategy = db.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () =>
                {

                    

                    await db.SaveChangesAsync();

                    await _events.PublishAsync(new EpisodeViewedEvent
                    {
                        ViewerId = userId,
                        EpisodeId = episodeId,
                        CreatorId = (Guid)creatorId
                    });

                    return true;
                });
                return false;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to track view for episode {EpisodeId} by user {UserId}.", episodeId, userId);
            }
        }
        

        return false;
    }

    public async Task<bool> DeleteEpisodeAsync(int episodeId, Guid userId, string role)
    {
        try
        {
            var db = shardRouter.GetDbForUser(userId);
            var strategy = db.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                var episode = await db.ScamflixEpisodes
                    .Include(e => e.Creator)
                    .FirstOrDefaultAsync(e => e.Id == episodeId);

                if (episode != null)
                {

                    // Only allow if admin or owner
                    if (role != RoleNames.Admin || episode.CreatedBy != userId)
                        return false;

                    db.ScamflixEpisodes.Remove(episode);
                    await db.SaveChangesAsync();

                    // Delete from Cloudinary
                    await _cloudinaryService.DeleteImageFromCloudinary(episode.ImageUrl);

                    return true;
                }

                return false;

            });
            
        }
        catch (Exception e)
        {
            _logger.LogError("failed to delete episode {ID}", episodeId);
            throw;
        }

        return false;
    }

}