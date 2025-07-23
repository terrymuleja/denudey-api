using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Denudey.Api.Domain.DTOs;
using Denudey.Api.Domain.Entities;
using Denudey.Api.Domain.Models;
using Denudey.Api.Models;
using Denudey.Api.Services.Cloudinary.Interfaces;
using Denudey.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Denudey.Api.Services.Implementations
{
    public class EpisodesService (ApplicationDbContext db, ICloudinaryService cloudinaryService) : IEpisodesService
    {

        public async Task<PagedResult<ScamFlixEpisodeDto>> GetEpisodesAsync(Guid? createdBy, Guid? currentUserId, string? search, int page, int pageSize)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 100) pageSize = 10;

            var query = db.ScamflixEpisodes
                .Include(e => e.Creator)
                .Include(e => e.Likes)
                .Include(e => e.Views)
                .AsQueryable();

            if (createdBy.HasValue)
            {
                query = query.Where(e => e.CreatedBy == createdBy);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.ToLower();
                query = query.Where(e =>
                    e.Title.ToLower().Contains(keyword) ||
                    e.Tags.ToLower().Contains(keyword));
            }

            var totalItems = await query.CountAsync();
            var episodes = await query
                .OrderByDescending(e => e.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new ScamFlixEpisodeDto
                {
                    Id = e.Id,
                    Title = e.Title,
                    Tags = e.Tags,
                    ImageUrl = e.ImageUrl,
                    CreatedAt = e.CreatedAt,
                    CreatedBy = e.Creator.Username,
                    CreatorId = e.Creator.Id.ToString(),
                    CreatorAvatarUrl = e.Creator.ProfileImageUrl ?? string.Empty,
                    Likes = e.Likes.Count,
                    Views = e.Views.Count,
                    HasUserLiked = currentUserId != null && e.Likes.Any(l => l.UserId == currentUserId)
                })
                .ToListAsync();

            return new PagedResult<ScamFlixEpisodeDto>
            {
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize,
                Items = episodes,
                HasNextPage = (page * pageSize) < totalItems
            };
        }

        public async Task<bool> DeleteEpisodeAsync(int episodeId, Guid userId, string role)
        {
            var episode = await db.ScamflixEpisodes.FindAsync(episodeId);
            if (episode == null)
                return false;

            // Only allow if admin or owner
            if (role != RoleNames.Admin && episode.CreatedBy != userId)
                return false;

            db.ScamflixEpisodes.Remove(episode);
            await db.SaveChangesAsync();



            //Cloudinary deletion
            await cloudinaryService.DeleteImageFromCloudinary(episode.ImageUrl);

            return true;
        }


        public async Task<(bool HasUserLiked, int Likes)> ToggleLikeAsync(int episodeId, Guid userId)
        {
            if (episodeId <= 0)
                throw new ArgumentException("Episode ID must be greater than zero.", nameof(episodeId));

            if (userId == Guid.Empty)
                throw new ArgumentException("User ID cannot be empty.", nameof(userId));

            try
            {
                bool hasUserLiked;
                var strategy = db.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () =>
                {
                    var existing = await db.EpisodeLikes
                        .FirstOrDefaultAsync(l => l.EpisodeId == episodeId && l.UserId == userId);

                    if (existing != null)
                    {
                        db.EpisodeLikes.Remove(existing);
                        hasUserLiked = false;
                    }
                    else
                    {
                        var episodeExists = await db.ScamflixEpisodes.AnyAsync(e => e.Id == episodeId);
                        if (!episodeExists)
                            throw new InvalidOperationException($"Episode with ID {episodeId} does not exist.");

                        db.EpisodeLikes.Add(new EpisodeLike
                        {
                            EpisodeId = episodeId,
                            UserId = userId,
                            LikedAt = DateTime.UtcNow
                        });
                        hasUserLiked = true;
                    }

                    await db.SaveChangesAsync();
                });

                var likeCount = await db.EpisodeLikes.CountAsync(l => l.EpisodeId == episodeId);
                var hasLiked = await db.EpisodeLikes.AnyAsync(l => l.EpisodeId == episodeId && l.UserId == userId);
                return (hasLiked, likeCount);
            }
            catch (DbUpdateException ex)
            {
                if (ex.InnerException?.Message?.Contains("FOREIGN KEY constraint") == true)
                    throw new InvalidOperationException("Invalid episode or user reference.", ex);

                throw new InvalidOperationException("An error occurred while updating the like status.", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("An unexpected error occurred while processing the like toggle.", ex);
            }
        }

        public async Task<bool> TrackViewAsync(int episodeId, Guid userId)
        {
            try
            {
                var success = false;
                var strategy = db.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () =>
                {
                    db.EpisodeViews.Add(new EpisodeView
                    {
                        EpisodeId = episodeId,
                        UserId = userId,
                        ViewedAt = DateTime.UtcNow
                    });
                    success = await db.SaveChangesAsync() > 0;
                });


                return success;
            }
            catch (DbUpdateException ex)
            {
                // Optional: Log detailed DB exception here
                Console.WriteLine($"Database update failed in TrackViewAsync: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("💥 General Exception caught:");
                Console.WriteLine(ex.ToString()); // Full type + message + stack
                Console.WriteLine("💬 Inner Exception:");
                Console.WriteLine(ex.InnerException?.ToString());
                Console.WriteLine($"IsConnected: {db.Database.CanConnect()}");

                // Optional: Log general exception
                Console.WriteLine($"Unexpected error in TrackViewAsync: {ex.Message}");
                return false;
            }
        }


    }

}
