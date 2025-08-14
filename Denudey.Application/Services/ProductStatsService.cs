using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CloudinaryDotNet.Core;
using Denudey.Api.Domain.DTOs;
using Denudey.Api.Services.Infrastructure.DbContexts;
using Denudey.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Denudey.Application.Services
{
    public class ProductStatsService(StatsDbContext statsDb, ILogger<ProductStatsService> logger) : IProductStatsService
    {
        public async Task<string> GetCreatorAvatarUrl(Guid userId)
        {
            var user = await statsDb.CreatorSocials.FindAsync(userId);
            if (user != null)
            {
                return user.ProfileImageUrl ?? "";
            }

            return "";
        }

        public async Task<Dictionary<Guid, ProductStatsDto>> GetStatsForProductsAsync(List<Guid> productIds, Guid? userId)
        {
            var result = new Dictionary<Guid, ProductStatsDto>();
            try
            {              
                // Load view counts
                var views = await statsDb.ProductViews
                    .Where(v => productIds.Contains(v.ProductId))
                    .GroupBy(v => v.ProductId)
                    .Select(g => new { ProductId = g.Key, Count = g.Count() })
                    .ToListAsync();

                // Load like counts     
                var likes = await statsDb.ProductLikes
                    .Where(l => productIds.Contains(l.ProductId))
                    .GroupBy(l => l.ProductId)
                    .Select(g => new { ProductId = g.Key, Count = g.Count() })
                    .ToListAsync();

                // Load current user's likes
                var likedByUser = userId == null
                    ? new List<Guid>()
                    : await statsDb.ProductLikes
                        .Where(l => l.UserId == userId && productIds.Contains(l.ProductId))
                        .Select(l => l.ProductId)
                        .ToListAsync();

                // Merge results
                
                foreach (var id in productIds)
                {
                    result[id] = new ProductStatsDto
                    {
                        ProductId = id,
                        Views = views.FirstOrDefault(v => v.ProductId == id)?.Count ?? 0,
                        Likes = likes.FirstOrDefault(l => l.ProductId == id)?.Count ?? 0,
                        UserHasLiked = likedByUser.Contains(id)
                    };
                }

                

            }
            catch (Exception exc)
            {
                logger.LogCritical(exc, exc.Message);
                throw;
            }
            return result;
        }
    }

}
