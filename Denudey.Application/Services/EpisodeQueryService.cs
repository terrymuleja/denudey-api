using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Denudey.Api.Domain.DTOs;
using Denudey.Api.Models;
using Denudey.Api.Services.Infrastructure.Sharding;
using Denudey.Application.Interfaces;
using Elastic.Clients.Elasticsearch;
using Microsoft.EntityFrameworkCore;

namespace Denudey.Application.Services
{


    public class EpisodeQueryService(IShardRouter router, IEpisodeStatsService stats)
    {
        public async Task<PagedResult<ScamFlixEpisodeDto>> GetMyEpisodes(
       Guid currentUserId,
       string? search,
       int page,
       int pageSize)
        {
            var db = router.GetDbForUser(currentUserId);

            var query = db.ScamflixEpisodes
                .Include(e => e.Creator)
                .Where(e => e.Creator.Id == currentUserId);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(e => e.Title.Contains(search));

            var totalCount = await query.CountAsync();

            var episodes = await query
                .OrderByDescending(e => e.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var episodeIds = episodes.Select(e => e.Id).ToList();
            var statsMap = await stats.GetStatsForEpisodesAsync(episodeIds, currentUserId);

            var items = episodes.Select(e =>
            {
                statsMap.TryGetValue(e.Id, out var stat);
                return new ScamFlixEpisodeDto
                {
                    Id = e.Id,
                    Title = e.Title,
                    Tags = e.Tags,
                    ImageUrl = e.ImageUrl,
                    CreatedAt = e.CreatedAt,
                    CreatorId = e.CreatedBy,
                    CreatedBy = e.Creator.Username,
                    CreatorAvatarUrl = e.Creator.ProfileImageUrl ?? "",
                    Likes = stat?.Likes ?? 0,
                    Views = stat?.Views ?? 0,
                    HasUserLiked = stat?.UserHasLiked ?? false
                };
            }).ToList();

            return new PagedResult<ScamFlixEpisodeDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalCount
            };
        }


    }
}
