using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Denudey.Api.Domain.DTOs;
using Denudey.Api.Domain.Models;
using Denudey.Api.Models;
using Denudey.Api.Services.Cloudinary.Interfaces;
using Denudey.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Denudey.Api.Services.Implementations
{
    public class EpisodesService (ApplicationDbContext db, ICloudinaryService cloudinaryService) : IEpisodesService
    {

        public async Task<PagedResult<ScamFlixEpisodeDto>> GetEpisodesAsync(Guid? createdBy, string? search, int page, int pageSize)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 100) pageSize = 10;

            var query = db.ScamflixEpisodes
                .Include(e => e.Creator)
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
                    CreatorId = e.Creator.Id.ToString()
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

        public async Task<bool> DeleteEpisodeAsync(Guid episodeId, Guid userId, string role)
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

    }

}
