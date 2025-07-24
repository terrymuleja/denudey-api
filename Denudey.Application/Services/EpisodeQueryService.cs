using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Denudey.Api.Domain.DTOs;
using Denudey.Api.Models;
using Denudey.Api.Services.Infrastructure.Sharding;
using Microsoft.EntityFrameworkCore;

namespace Denudey.Application.Services
{


    public class EpisodeQueryService(IShardRouter shardRouter) 
    {
        public async Task<List<ScamFlixEpisodeDto>> GetEpisodesAsync(
            Guid? createdBy, 
            Guid? currentUserId, 
            string? search, 
            int page, 
            int pageSize)
        {
            var db = shardRouter.GetDbForUser(currentUserId ?? Guid.Empty);

            var query = db.ScamflixEpisodes
                .Include(e => e.Creator)
                .AsQueryable();

            if (createdBy != null)
                query = query.Where(e => e.Creator.Id == createdBy);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(e => e.Title.Contains(search));

            return await query
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
                    CreatorId = e.Creator.Id.ToString(),
                    CreatorAvatarUrl = e.Creator.ProfileImageUrl ?? ""
                })
                .ToListAsync();
        }

        public async Task<Guid?> GetCreatorIdAsync(int episodeId, Guid currentUserId)
        {
            var db = shardRouter.GetDbForUser(currentUserId);

            return await db.ScamflixEpisodes
                .Where(e => e.Id == episodeId)
                .Select(e => e.Creator.Id)
                .FirstOrDefaultAsync();
        }
    }
}
