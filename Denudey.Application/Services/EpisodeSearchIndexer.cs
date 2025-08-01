﻿using System;
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

namespace Denudey.Application.Services
{
    public class EpisodeSearchIndexer(ElasticsearchClient elastic, IEpisodeStatsService stats) : IEpisodeSearchIndexer
    {
        public async Task IndexAsync(ScamflixEpisode episode)
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

        public async Task DeleteAsync(int episodeId)
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

            // Handle search query - this was the main issue
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
                // When no search term, return all episodes
                searchRequest.Query = new MatchAllQuery();
            }

            var response = await elastic.SearchAsync<ScamFlixEpisodeSearchDto>(searchRequest);

            if (!response.IsValidResponse)
                throw new Exception("Elastic search failed: " + response.DebugInformation);

            var hits = response.Documents.ToList();
            var episodeIds = hits.Select(e => e.Id).ToList();
            var statsMap = await stats.GetStatsForEpisodesAsync(episodeIds, currentUserId);

            var items = hits.Select(e =>
            {
                statsMap.TryGetValue(e.Id, out var stat);
                return new ScamFlixEpisodeDto
                {
                    Id = e.Id,
                    Title = e.Title,
                    Tags = string.Join(", ", e.Tags),
                    ImageUrl = e.ImageUrl,
                    CreatedAt = e.CreatedAt,
                    CreatorId = e.CreatorId,
                    CreatedBy = e.CreatorUsername,
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
                TotalItems = (int)response.Total
            };
        }
    }
}