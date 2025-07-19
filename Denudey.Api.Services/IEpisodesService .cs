using Denudey.Api.Domain.DTOs;
using Denudey.Api.Models;

namespace Denudey.Api.Services
{
    public interface IEpisodesService
    {
        Task<PagedResult<ScamFlixEpisodeDto>> GetEpisodesAsync(Guid? createdBy, string? search, int page, int pageSize);
        Task<bool> DeleteEpisodeAsync(Guid episodeId, Guid userId, string role);


    }
}
