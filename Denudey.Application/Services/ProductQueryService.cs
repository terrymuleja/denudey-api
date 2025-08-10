using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Denudey.Api.Domain.DTOs;
using Denudey.Api.Domain.Entities;
using Denudey.Api.Models;
using Denudey.Api.Services.Infrastructure.Sharding;
using Denudey.Application.Interfaces;
using Elastic.Clients.Elasticsearch.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Denudey.Application.Services
{
    public class ProductQueryService
    {
        private readonly IShardRouter _router;
        private readonly IProductStatsService _stats;
        private readonly ILogger<ProductQueryService> _logger;

        public ProductQueryService(IShardRouter router, IProductStatsService stats, ILogger<ProductQueryService> logger)
        {
            _router = router ?? throw new ArgumentNullException(nameof(router));
            _stats = stats ?? throw new ArgumentNullException(nameof(stats));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<PagedResult<ProductDetailsDto>> GetMyProducts(
            Guid currentUserId,
            string? search,
            int page,
            int pageSize)
        {
            try
            {
                var db = _router.GetDbForUser(currentUserId);

                // Get all products for the user (only ~10 products)
                var allProducts = await db.Products
                    .Include(e => e.Creator)
                    .Where(e => e.Creator.Id == currentUserId)
                    .OrderByDescending(e => e.CreatedAt)
                    .ToListAsync();

                // Filter in memory if search is provided
                var filteredProducts = allProducts;
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var searchLower = search.ToLower();
                    filteredProducts = allProducts.Where(e =>
                        e.ProductName.ToLower().Contains(searchLower) ||
                        (e.Description != null && e.Description.ToLower().Contains(searchLower)) ||
                        (e.Tags != null && e.Tags.Any(t => t.ToLower().Contains(searchLower)))
                    ).ToList();
                }

                var totalCount = filteredProducts.Count;

                // Apply pagination
                var products = filteredProducts
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
           

                var productIds = products.Select(e => e.Id).ToList();
                Dictionary<Guid, ProductStatsDto> statsMap;
                try
                {
                    statsMap = await _stats.GetStatsForProductsAsync(productIds, currentUserId);
                }
                catch (Exception statsEx)
                {
                    _logger?.LogWarning(statsEx, "Failed to get product stats, using defaults");
                    statsMap = new Dictionary<Guid, ProductStatsDto>();
                }
                

                var items = products.Select(product =>
                {
                    statsMap.TryGetValue(product.Id, out var stat);
                    return new ProductDetailsDto
                    {
                        Id = product.Id,
                        ProductName = product.ProductName,
                        Description = product.Description ?? "",
                        Tags = product.Tags,
                        MainPhotoUrl = product.MainPhotoUrl ?? "",
                        SecondaryPhotoUrls = product.SecondaryPhotoUrls,
                        BodyPart = product.BodyPart,
                        DeliveryOptions = product.DeliveryOptions,
                        FeePerDelivery = product.FeePerDelivery,
                        IsPublished = product.IsPublished,
                        IsExpired = product.IsExpired,

                        CreatedBy = product.Creator?.Id ?? product.CreatedBy,
                        CreatorUsername = product.Creator?.Username ?? "unknown",
                        CreatorAvatarUrl = product.Creator?.ProfileImageUrl ?? string.Empty,
                        Likes = stat?.Likes ?? 0,
                        Views = stat?.Views ?? 0,
                        CreatedAt = product.CreatedAt,
                        ModifiedAt = product.ModifiedAt,
                    };
                }).ToList();

                return new PagedResult<ProductDetailsDto>
                {
                    Items = items,
                    Page = page,
                    PageSize = pageSize,
                    TotalItems = totalCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting episodes for user {UserId}", currentUserId);
                throw;
            }
        }

        public async Task<ProductDetailsDto?> GetProductDetailsAsync(Guid id, Guid currentUserId)
        {
            try
            {
                var db = _router.GetDbForUser(currentUserId);

                var product = await db.Products
                    .Include(p => p.Creator)
                    .FirstOrDefaultAsync(p => p.Id == id && p.CreatedBy == currentUserId);

                if (product == null)
                    return null;

                // Get stats using the stats service (like in GetMyProducts)
                Dictionary<Guid, ProductStatsDto> statsMap;
                try
                {
                    statsMap = await _stats.GetStatsForProductsAsync(new List<Guid> { id }, currentUserId);
                }
                catch (Exception statsEx)
                {
                    _logger?.LogWarning(statsEx, "Failed to get product stats for {ProductId}, using defaults", id);
                    statsMap = new Dictionary<Guid, ProductStatsDto>();
                }

                statsMap.TryGetValue(id, out var stats);

                return new ProductDetailsDto
                {
                    Id = product.Id,
                    ProductName = product.ProductName,
                    Description = product.Description ?? "",
                    Tags = product.Tags,
                    MainPhotoUrl = product.MainPhotoUrl ?? "",
                    SecondaryPhotoUrls = product.SecondaryPhotoUrls,
                    BodyPart = product.BodyPart,
                    DeliveryOptions = product.DeliveryOptions,
                    FeePerDelivery = product.FeePerDelivery,
                    IsPublished = product.IsPublished,
                    IsExpired = product.IsExpired,
                    CreatedAt = product.CreatedAt,
                    ModifiedAt = product.ModifiedAt,
                    CreatedBy = product.Creator?.Id ?? product.CreatedBy,
                    CreatorUsername = product.Creator?.Username ?? "unknown",
                    CreatorAvatarUrl = product.Creator?.ProfileImageUrl ?? string.Empty,
                    Views = stats?.Views ?? 0,
                    Likes = stats?.Likes ?? 0,
                    HasUserLiked = stats?.UserHasLiked ?? false
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product details for {ProductId}", id);
                throw;
            }
        }

    }
}