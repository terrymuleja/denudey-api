using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Denudey.Api.Domain.DTOs;
using Denudey.Api.Services.Infrastructure.DbContexts;
using Denudey.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Denudey.Application.Services
{
    public class EpisodeStatsService(StatsDbContext statsDb) : IEpisodeStatsService
    {

        public async Task<string> GetRequestorAvatarUrl(Guid userId)
        {
            var user = await statsDb.RequesterSocials.FirstOrDefaultAsync(c => c.RequesterId == userId );
            if (user != null)
            {
                return user.ProfileImageUrl ?? "";
            }

            return "";
        }


        public async Task<Dictionary<int, EpisodeStatsDto>> GetStatsForEpisodesAsync(List<int> episodeIds, Guid? userId)
        {
            // Load view counts
            var views = await statsDb.EpisodeViews
                .Where(v => episodeIds.Contains(v.EpisodeId))
                .GroupBy(v => v.EpisodeId)
                .Select(g => new { EpisodeId = g.Key, Count = g.Count() })
                .ToListAsync();

            // Load like counts
            var likes = await statsDb.EpisodeLikes
                .Where(l => episodeIds.Contains(l.EpisodeId))
                .GroupBy(l => l.EpisodeId)
                .Select(g => new { EpisodeId = g.Key, Count = g.Count() })
                .ToListAsync();

            // Load current user's likes
            var likedByUser = userId == null
                ? new List<int>()
                : await statsDb.EpisodeLikes
                    .Where(l => l.UserId == userId && episodeIds.Contains(l.EpisodeId))
                    .Select(l => l.EpisodeId)
                    .ToListAsync();

            // Merge results
            var result = new Dictionary<int, EpisodeStatsDto>();
            foreach (var id in episodeIds)
            {
                result[id] = new EpisodeStatsDto
                {
                    EpisodeId = id,
                    Views = views.FirstOrDefault(v => v.EpisodeId == id)?.Count ?? 0,
                    Likes = likes.FirstOrDefault(l => l.EpisodeId == id)?.Count ?? 0,
                    UserHasLiked = likedByUser.Contains(id)
                };
            }

            return result;
        }
    }

}
