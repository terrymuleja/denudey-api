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

        // Updated SearchProductsAsync method in ProductSearchIndexer
        // Updated SearchProductsAsync method in ProductSearchIndexer
        public async Task<PagedResult<ProductDetailsDto>> SearchProductsAsync(
    string? search,
    Guid? currentUserId,
    int page,
    int pageSize,
    string[]? bodyParts = null)
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

                // Build the query with multiple conditions
                var queries = new List<Query>();

                // ALWAYS filter for published products only
                queries.Add(new TermQuery
                {
                    Field = "isPublished",
                    Value = true
                });

                // ALWAYS filter out expired products
                queries.Add(new TermQuery
                {
                    Field = "isExpired",
                    Value = false
                });

                // Handle search query with partial matching
                if (!string.IsNullOrWhiteSpace(search))
                {
                    queries.Add(new BoolQuery
                    {
                        Should = new Query[]
                        {
                    // Wildcard search for partial matching in product name (highest priority)
                    new WildcardQuery
                    {
                        Field = "productName",
                        Value = $"*{search.ToLower()}*",
                        Boost = 3.0f,
                        CaseInsensitive = true
                    },
                    
                    // Wildcard search for partial matching in description
                    new WildcardQuery
                    {
                        Field = "description",
                        Value = $"*{search.ToLower()}*",
                        Boost = 2.0f,
                        CaseInsensitive = true
                    },
                    
                    // Standard match for tags (tags usually work well with exact matching)
                    new MatchQuery
                    {
                        Field = "tags",
                        Query = search,
                        Boost = 2.5f,
                        Operator = Operator.Or
                    },
                    
                    // Standard match for creator username
                    new MatchQuery
                    {
                        Field = "creatorUsername",
                        Query = search,
                        Boost = 1.0f,
                        Operator = Operator.Or
                    },
                    
                    // Fuzzy matching for typos in product name
                    new FuzzyQuery
                    {
                        Field = "productName",
                        Value = search.ToLower(),
                        Boost = 1.5f,
                        Fuzziness = new Fuzziness("AUTO")
                    },
                    
                    // Query string for natural search behavior (fallback)
                    new QueryStringQuery
                    {
                        Query = $"*{search}*",
                        Fields = new[] { "productName^2", "description^1.5", "tags^2", "creatorUsername^1" },
                        DefaultOperator = Operator.Or,
                        Boost = 1.0f
                    }
                        },
                        MinimumShouldMatch = 1
                    });
                }

                // Handle body parts filter
                if (bodyParts != null && bodyParts.Length > 0)
                {
                    // Filter out any null/empty values
                    var validBodyParts = bodyParts.Where(bp => !string.IsNullOrWhiteSpace(bp)).ToArray();

                    if (validBodyParts.Length > 0)
                    {
                        queries.Add(new TermsQuery
                        {
                            Field = "bodyPart.keyword", // Use .keyword for exact matching
                            Terms = new TermsQueryField(validBodyParts.Select(bp => FieldValue.String(bp.ToLowerInvariant())).ToArray())
                        });
                    }
                }

                // Always use BoolQuery with Must to ensure all conditions are met
                searchRequest.Query = new BoolQuery
                {
                    Must = queries
                };

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

                    return new PagedResult<ProductDetailsDto>
                    {
                        Items = new List<ProductDetailsDto>(),
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
                    return new PagedResult<ProductDetailsDto>
                    {
                        Items = new List<ProductDetailsDto>(),
                        Page = page,
                        PageSize = pageSize,
                        TotalItems = 0,
                        HasNextPage = false
                    };
                }

                // Process the results (rest of the method remains the same)
                var productIds = hits.Select(e => e.Id).ToList();

                Dictionary<Guid, ProductStatsDto> statsMap;
                try
                {
                    statsMap = await stats.GetStatsForProductsAsync(productIds, currentUserId);
                }
                catch (Exception statsEx)
                {
                    logger?.LogWarning(statsEx, "Failed to get product stats, using defaults");
                    statsMap = new Dictionary<Guid, ProductStatsDto>();
                }

                var items = hits.Select(e =>
                {
                    statsMap.TryGetValue(e.Id, out var stat);
                    return new ProductDetailsDto
                    {
                        Id = e.Id,
                        ProductName = e.ProductName ?? "Untitled",
                        Description = e.Description,
                        BodyPart = e.BodyPart,
                        Tags = e.Tags,
                        MainPhotoUrl = e.MainPhotoUrl ?? "",
                        SecondaryPhotoUrls = e.SecondaryPhotoUrls,
                        DeliveryOptions = e.DeliveryOptions,
                        FeePerDelivery = e.FeePerDelivery,
                        CreatorUsername = e.CreatorUsername ?? "Unknown",
                        CreatedAt = e.CreatedAt,

                        CreatedBy = e.CreatedBy,
                        CreatorAvatarUrl = stats.GetCreatorAvatarUrl(e.CreatedBy).Result,
                        Likes = stat?.Likes ?? 0,
                        Views = stat?.Views ?? 0,
                        HasUserLiked = stat?.UserHasLiked ?? false
                    };
                }).ToList();

                return new PagedResult<ProductDetailsDto>
                {
                    TotalItems = totalItems,
                    Items = items.ToList(),
                    HasNextPage = totalItems > (page * pageSize),
                    Page = page,
                    PageSize = pageSize
                };
            }
            catch (Exception ex)
            {
                // Log the error but return empty result instead of throwing
                logger?.LogError(ex, "Exception in SearchProductsAsync - returning empty result");

                return new PagedResult<ProductDetailsDto>
                {
                    Items = new List<ProductDetailsDto>(),
                    Page = page,
                    PageSize = pageSize,
                    TotalItems = 0,
                    HasNextPage = false
                };
            }
        }
        public async Task<ProductDetailsDto> GetProductByIdAsync(Guid productId, Guid? currentUserId)
        {
            try
            {
                var response = await elastic.GetAsync<ProductDetailsDto>(productId, idx => idx
                    .Index(_indexName)); // Replace with your actual index name

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

                    return null;
                }

                // Handle empty results (this is normal, not an error)
                var source = response.Source;
              
                
                // Handle stats service gracefully too
                Dictionary<Guid, ProductStatsDto> statsMap;
                try
                {
                    statsMap = await stats.GetStatsForProductsAsync(new List<Guid> { source.Id }, currentUserId);
                }
                catch (Exception statsEx)
                {
                    logger?.LogWarning(statsEx, "Failed to get episode stats, using defaults");
                    statsMap = new Dictionary<Guid, ProductStatsDto>();
                }

                statsMap.TryGetValue(source.Id, out var stat);
                var items = new List<ProductDetailsDto> {
                
                };
                var product = new ProductDetailsDto
                {
                    Id = source.Id,
                    ProductName = source.ProductName ?? "Untitled",
                    Description = source.Description,
                    BodyPart = source.BodyPart,
                    Tags = source.Tags,
                    MainPhotoUrl = source.MainPhotoUrl ?? "",
                    SecondaryPhotoUrls = source.SecondaryPhotoUrls,
                    DeliveryOptions = source.DeliveryOptions,
                    FeePerDelivery = source.FeePerDelivery,
                    CreatorUsername = source.CreatorUsername ?? "Unknown",
                    CreatedAt = source.CreatedAt,

                    CreatedBy = source.CreatedBy,
                    CreatorAvatarUrl = source.CreatorAvatarUrl ?? "",
                    Likes = stat?.Likes ?? 0,
                    Views = stat?.Views ?? 0,
                    HasUserLiked = stat?.UserHasLiked ?? false
                };
                return product;
            }
            catch (Exception ex)
            {
                // Handle exception
                logger.LogError(ex, ex.Message);
                return null;
            }
        }
    }
}