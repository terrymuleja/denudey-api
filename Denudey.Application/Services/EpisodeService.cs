using Denudey.Api.Domain.Entities;
using Denudey.Api.Domain.Events;
using Denudey.Api.Services.Cloudinary;
using Denudey.Api.Services.Cloudinary.Interfaces;
using Denudey.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

public class EpisodeService(IShardRouter router, IEventPublisher events, ICloudinaryService cloudinaryService)
{
    
    public async Task LikeEpisode(Guid userId, int episodeId, Guid creatorId)
    {
        var db = router.GetDbForUser(userId);

        bool alreadyLiked = await db.EpisodeLikes
            .AnyAsync(l => l.UserId == userId && l.EpisodeId == episodeId);

        if (alreadyLiked) return;


        db.EpisodeLikes.Add(new EpisodeLike()
        {
            UserId = userId,
            EpisodeId = episodeId,
            LikedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        await events.PublishAsync(new EpisodeLikedEvent()
        {
            LikerId = userId,
            EpisodeId = episodeId,
            CreatorId = creatorId
        });
    }

    public async Task TrackViewEpisode(Guid userId, int episodeId, Guid creatorId)
    {
        var db = router.GetDbForUser(userId);

        db.EpisodeViews.Add(new EpisodeView
        {
            UserId = userId,
            EpisodeId = episodeId,
            ViewedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        await events.PublishAsync(new EpisodeViewedEvent
        {
            ViewerId = userId,
            EpisodeId = episodeId,
            CreatorId = creatorId
        });
    }

    public async Task<bool> DeleteEpisodeAsync(int episodeId, Guid userId)
    {
        var db = router.GetDbForUser(userId);
        var episode = await db.ScamflixEpisodes
            .Include(e => e.Creator)
            .FirstOrDefaultAsync(e => e.Id == episodeId);

        if (episode == null)
            return false;

        if (episode.Creator.Id != userId)
            return false;

        db.ScamflixEpisodes.Remove(episode);
        await db.SaveChangesAsync();

        // Delete from Cloudinary
        await cloudinaryService.DeleteImageFromCloudinary(episode.ImageUrl);

        return true;
    }

}