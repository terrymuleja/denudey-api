using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Denudey.Api.Domain.DTOs;
using Denudey.Api.Domain.Entities;
using Denudey.Application.Interfaces;
using Elastic.Clients.Elasticsearch;

namespace Denudey.Application.Services
{
    public class EpisodeSearchIndexer(ElasticsearchClient elastic) : IEpisodeSearchIndexer
    {
        

        public async Task IndexAsync(ScamflixEpisode episode)
        {
            var dto = new ScamFlixEpisodeSearchDto
            {
                Id = episode.Id,
                Title = episode.Title,
                Tags = string.IsNullOrWhiteSpace(episode.Tags)
                        ? new List<string>()
                        : episode.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList(),
                ImageUrl = episode.ImageUrl,

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

        public async Task DeleteAsync(Guid episodeId)
        {
            var response = await elastic.DeleteAsync<ScamFlixEpisodeSearchDto>(episodeId, d => d
                .Index("scamflix_episodes")
            );

            if (!response.IsValidResponse && response.Result != Result.NotFound)
                throw new Exception($"Failed to delete episode: {response.DebugInformation}");
        }
    }

}
