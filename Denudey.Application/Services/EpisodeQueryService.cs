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


    public class EpisodeQueryService(IShardRouter router) : EpisodeServiceBase(router)
    {
        public async Task<PagedResult<ScamFlixEpisodeDto>> GetEpisodesAsync(
            Guid? createdBy,
            Guid? currentUserId,
            string? search,
            int page,
            int pageSize)
        {
            var db = shardRouter.GetDbForUser(currentUserId ?? Guid.Empty);

            var query = db.ScamflixEpisodes
                .Include(e => e.Creator)
                .Include(e  => e.Views)
                .Include(e => e.Likes)
                .AsQueryable();

            if (createdBy != null)
                query = query.Where(e => e.Creator.Id == createdBy);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(e => e.Title.Contains(search));

            var totalCount = await query.CountAsync();

            var items = await query
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
                    CreatorAvatarUrl = e.Creator.ProfileImageUrl ?? "",
                    CreatedBy = e.Creator.Username,
                    Likes = e.Likes.Count,
                    Views = e.Views.Count,
                    HasUserLiked = currentUserId != null && e.Likes.Any(l => l.UserId == currentUserId)
                })
                .ToListAsync();

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
