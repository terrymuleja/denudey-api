using Denudey.Api.Domain.DTOs;
using Denudey.Api.Models;

namespace Denudey.Api.Services
{
    public interface IEpisodesService
    {
        Task<PagedResult<ScamFlixEpisodeDto>> GetEpisodesAsync(Guid? createdBy, string? search, int page, int pageSize);
        Task<bool> DeleteEpisodeAsync(int episodeId, Guid userId, string role);

        Task<bool> AddViewAsync(int episodeId, Guid userId);
        Task<bool> ToggleLikeAsync(int episodeId, Guid userId);
        //Task<int> GetViewsAsync(int episodeId);
        //Task<int> GetLikesAsync(int episodeId);

    }
}
