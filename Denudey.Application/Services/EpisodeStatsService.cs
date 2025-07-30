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
        private readonly StatsDbContext _statsDb = statsDb;

        public async Task<Dictionary<Guid, EpisodeStatsDto>> GetStatsForEpisodesAsync(List<Guid> episodeIds, Guid? userId)
        {
            // Load view counts
            var views = await _statsDb.EpisodeViews
                .Where(v => episodeIds.Contains(v.EpisodeId))
                .GroupBy(v => v.EpisodeId)
                .Select(g => new { EpisodeId = g.Key, Count = g.Count() })
                .ToListAsync();

            // Load like counts
            var likes = await _statsDb.EpisodeLikes
                .Where(l => episodeIds.Contains(l.EpisodeId))
                .GroupBy(l => l.EpisodeId)
                .Select(g => new { EpisodeId = g.Key, Count = g.Count() })
                .ToListAsync();

            // Load current user's likes
            var likedByUser = userId == null
                ? new List<Guid>()
                : await _statsDb.EpisodeLikes
                    .Where(l => l.UserId == userId && episodeIds.Contains(l.EpisodeId))
                    .Select(l => l.EpisodeId)
                    .ToListAsync();

            // Merge results
            var result = new Dictionary<Guid, EpisodeStatsDto>();
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
