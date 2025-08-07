using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Denudey.Api.Domain.DTOs;
using Denudey.Api.Domain.Entities;
using Denudey.Api.Models;
using Denudey.Application.Interfaces;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Microsoft.Extensions.Logging;

namespace Denudey.Application.Services
{
    public class ProductSearchIndexer(ElasticsearchClient elastic, IProductStatsService stats, ILogger<ProductSearchIndexer> logger) : IProductSearchIndexer
    {
        private readonly string _indexName = "products";
        public async Task IndexProductAsync(Product product)
        {
            var dto = new ProductDetailsDto
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
                IsExpired   = product.IsExpired,

                CreatedBy = product.Creator?.Id ?? product.CreatedBy,
                CreatorUsername = product.Creator?.Username ?? "unknown",
                CreatorAvatarUrl = product.Creator?.ProfileImageUrl ?? string.Empty,
                CreatedAt = product.CreatedAt,
                ModifiedAt = product.ModifiedAt,
            };

            var response = await elastic.IndexAsync(dto, i => i
                .Index(_indexName)
                .Id(product.Id)
            );

            if (!response.IsValidResponse)
                throw new Exception($"Failed to index episode: {response.DebugInformation}");
        }

        public async Task DeleteProductFromIndexAsync(Guid productId)
        {
            var response = await elastic.DeleteAsync<Product>(productId, d => d
                .Index(_indexName)
            );

            if (!response.IsValidResponse && response.Result != Result.NotFound)
            {
                //logger.LogWarning("Failed to delete product: {productName}", )
                throw new Exception($"Failed to delete episode: {response.DebugInformation}");
            }
        }

        public async Task<PagedResult<ProductSummaryDto>> SearchProductsAsync(
    string? search,
    Guid? currentUserId,
    int page,
    int pageSize)
        {
            try
            {
                // Build the search request
                var searchRequest = new SearchRequest<ProductDetailsDto>(_indexName)
                {
                    From = (page - 1) * pageSize,
                    Size = pageSize,
                    Sort = new[]
                    {
                new SortOptions
                {
                    Field = new FieldSort(new Field("createdAt")) { Order = SortOrder.Desc }
                }
            }
                };

                // Handle search query
                if (!string.IsNullOrWhiteSpace(search))
                {
                    searchRequest.Query = new MultiMatchQuery
                    {
                        Query = search,
                        Fields = new[] { "productname", "description", "tags", "creatorUsername" }
                    };
                }
                else
                {
                    searchRequest.Query = new MatchAllQuery();
                }

                var response = await elastic.SearchAsync<ProductDetailsDto>(searchRequest);

                // Handle Elasticsearch errors properly
                if (!response.IsValidResponse)
                {
                    // Log the error but don't throw - return empty result
                    logger?.LogWarning("Elasticsearch search failed: {Error}", response.DebugInformation);

                    // Check if it's an index not found error (common when no data exists)
                    if (response.ApiCallDetails?.HttpStatusCode == 404)
                    {
                        logger?.LogInformation("Elasticsearch index '{indexName}' not found - returning empty result", _indexName);
                    }

                    return new PagedResult<ProductSummaryDto>
                    {
                        Items = new List<ProductSummaryDto>(),
                        Page = page,
                        PageSize = pageSize,
                        TotalItems = 0,
                        HasNextPage = false
                    };
                }

                // Handle empty results (this is normal, not an error)
                var hits = response.Documents?.ToList() ?? new List<ProductDetailsDto>();
                var totalItems = (int)response.Total;

                if (!hits.Any())
                {
                    // This is perfectly normal - just no episodes match the criteria
                    return new PagedResult<ProductSummaryDto>
                    {
                        Items = new List<ProductSummaryDto>(),
                        Page = page,
                        PageSize = pageSize,
                        TotalItems = 0,
                        HasNextPage = false
                    };
                }

                // Process the results
                var productIds = hits.Select(e => e.Id).ToList();

                // Handle stats service gracefully too
                Dictionary<Guid, ProductStatsDto> statsMap;
                try
                {
                    statsMap = await stats.GetStatsForProductsAsync(productIds, currentUserId);
                }
                catch (Exception statsEx)
                {
                    logger?.LogWarning(statsEx, "Failed to get episode stats, using defaults");
                    statsMap = new Dictionary<Guid, ProductStatsDto>();
                }

                var items = hits.Select(e =>
                {
                    statsMap.TryGetValue(e.Id, out var stat);
                    return new ProductDetailsDto
                    {
                        Id = e.Id,
                        ProductName = e.ProductName ?? "Untitled",
                        Tags = e.Tags,
                        MainPhotoUrl = e.MainPhotoUrl?? "",
                        SecondaryPhotoUrls =e.SecondaryPhotoUrls, 
                        CreatorUsername = e.CreatorUsername ?? "Unknown",
                        CreatedAt = e.CreatedAt,
                        
                        CreatedBy = e.CreatedBy,
                        CreatorAvatarUrl = e.CreatorAvatarUrl ?? "",
                        Likes = stat?.Likes ?? 0,
                        Views = stat?.Views ?? 0,
                        HasUserLiked = stat?.UserHasLiked ?? false
                    };
                }).ToList();

                return new PagedResult<ProductSummaryDto>
                {
                    Items = items.Select(p => p.ToSummary()).ToList(),
                    HasNextPage = totalItems > (page * pageSize)
                };
            }
            catch (Exception ex)
            {
                // Log the error but return empty result instead of throwing
                logger?.LogError(ex, "Exception in SearchEpisodesAsync - returning empty result");

                return new PagedResult<ProductSummaryDto>
                {
                    Items = new List<ProductSummaryDto>(),
                    Page = page,
                    PageSize = pageSize,
                    TotalItems = 0,
                    HasNextPage = false
                };
            }
        }
    }
}