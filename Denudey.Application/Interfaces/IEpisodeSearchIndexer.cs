using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Denudey.Api.Domain.Entities;

namespace Denudey.Application.Interfaces
{
    public interface IEpisodeSearchIndexer
    {
        Task IndexAsync(ScamflixEpisode episode);
        Task DeleteAsync(Guid episodeId);
    }

}
