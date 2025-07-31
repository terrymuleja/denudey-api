using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Denudey.Api.Domain.DTOs;
using Denudey.Api.Domain.Entities;
using Denudey.Api.Models;

namespace Denudey.Application.Interfaces
{
    public interface IEpisodeSearchIndexer
    {
        Task IndexAsync(ScamflixEpisode episode);
        Task DeleteAsync(int episodeId);
        Task<PagedResult<ScamFlixEpisodeDto>> SearchEpisodesAsync(
        string? search,
        Guid? currentUserId,
        int page,
        int pageSize);
    }

}
