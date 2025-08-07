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
    public class EpisodeSearchIndexer(ElasticsearchClient elastic, IEpisodeStatsService stats, ILogger<EpisodeSearchIndexer> logger) : IEpisodeSearchIndexer
    {
        public async Task IndexEpisodeAsync(ScamflixEpisode episode)
        {
            var dto = new ScamFlixEpisodeSearchDto
            {
                Id = episode.Id,
                Title = episode.Title ?? "",
                Tags = string.IsNullOrWhiteSpace(episode.Tags)
                        ? new List<string>()
                        : episode.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList(),
                ImageUrl = episode.ImageUrl ?? "",
                CreatorId = episode.Creator?.Id ?? episode.CreatedBy,
                CreatorUsername = episode.Creator?.Username ?? "unknown",
                CreatorAvatarUrl = episode.Creator?.ProfileImageUrl ?? string.Empty,
                CreatedAt = episode.CreatedAt
            };

            var response = await elastic.IndexAsync(dto, i => i
                .Index("scamflix_episodes")
                .Id(episode.Id)
            );

            if (!response.IsValidResponse)
                throw new Exception($"Failed to index episode: {response.DebugInformation}");
        }

        public async Task DeleteEpisodeFromIndexAsync(int episodeId)
        {
            var response = await elastic.DeleteAsync<ScamFlixEpisodeSearchDto>(episodeId, d => d
                .Index("scamflix_episodes")
            );

            if (!response.IsValidResponse && response.Result != Result.NotFound)
                throw new Exception($"Failed to delete episode: {response.DebugInformation}");
        }

        public async Task<PagedResult<ScamFlixEpisodeDto>> SearchEpisodesAsync(
    string? search,
    Guid? currentUserId,
    int page,
    int pageSize)
        {
            try
            {
                // Build the search request
                var searchRequest = new SearchRequest<ScamFlixEpisodeSearchDto>("scamflix_episodes")
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
                        Fields = new[] { "title", "tags", "creatorUsername" }
                    };
                }
                else
                {
                    searchRequest.Query = new MatchAllQuery();
                }

                var response = await elastic.SearchAsync<ScamFlixEpisodeSearchDto>(searchRequest);

                // Handle Elasticsearch errors properly
                if (!response.IsValidResponse)
                {
                    // Log the error but don't throw - return empty result
                    logger?.LogWarning("Elasticsearch search failed: {Error}", response.DebugInformation);

                    // Check if it's an index not found error (common when no data exists)
                    if (response.ApiCallDetails?.HttpStatusCode == 404)
                    {
                        logger?.LogInformation("Elasticsearch index 'scamflix_episodes' not found - returning empty result");
                    }

                    return new PagedResult<ScamFlixEpisodeDto>
                    {
                        Items = new List<ScamFlixEpisodeDto>(),
                        Page = page,
                        PageSize = pageSize,
                        TotalItems = 0,
                        HasNextPage = false
                    };
                }

                // Handle empty results (this is normal, not an error)
                var hits = response.Documents?.ToList() ?? new List<ScamFlixEpisodeSearchDto>();
                var totalItems = (int)response.Total;

                if (!hits.Any())
                {
                    // This is perfectly normal - just no episodes match the criteria
                    return new PagedResult<ScamFlixEpisodeDto>
                    {
                        Items = new List<ScamFlixEpisodeDto>(),
                        Page = page,
                        PageSize = pageSize,
                        TotalItems = 0,
                        HasNextPage = false
                    };
                }

                // Process the results
                var episodeIds = hits.Select(e => e.Id).ToList();

                // Handle stats service gracefully too
                Dictionary<int, EpisodeStatsDto> statsMap;
                try
                {
                    statsMap = await stats.GetStatsForEpisodesAsync(episodeIds, currentUserId);
                }
                catch (Exception statsEx)
                {
                    logger?.LogWarning(statsEx, "Failed to get episode stats, using defaults");
                    statsMap = new Dictionary<int, EpisodeStatsDto>();
                }

                var items = hits.Select(e =>
                {
                    statsMap.TryGetValue(e.Id, out var stat);
                    return new ScamFlixEpisodeDto
                    {
                        Id = e.Id,
                        Title = e.Title ?? "Untitled",
                        Tags = string.Join(", ", e.Tags ?? new List<string>()),
                        ImageUrl = e.ImageUrl ?? "",
                        CreatedAt = e.CreatedAt,
                        CreatorId = e.CreatorId,
                        CreatedBy = e.CreatorUsername ?? "Unknown",
                        CreatorAvatarUrl = e.CreatorAvatarUrl ?? "",
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
                    TotalItems = totalItems,
                    HasNextPage = totalItems > (page * pageSize)
                };
            }
            catch (Exception ex)
            {
                // Log the error but return empty result instead of throwing
                logger?.LogError(ex, "Exception in SearchEpisodesAsync - returning empty result");

                return new PagedResult<ScamFlixEpisodeDto>
                {
                    Items = new List<ScamFlixEpisodeDto>(),
                    Page = page,
                    PageSize = pageSize,
                    TotalItems = 0,
                    HasNextPage = false
                };
            }
        }
    }
}