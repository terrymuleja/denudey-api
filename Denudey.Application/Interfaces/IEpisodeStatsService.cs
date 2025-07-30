using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Denudey.Api.Domain.DTOs;

namespace Denudey.Application.Interfaces
{
    public interface IEpisodeStatsService
    {
        Task<Dictionary<Guid, EpisodeStatsDto>> GetStatsForEpisodesAsync(List<Guid> episodeIds, Guid? userId);
    }

}
