using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Denudey.Application.Services
{
    public class EpisodeServiceBase
    {
        protected readonly IShardRouter shardRouter;

        public EpisodeServiceBase(IShardRouter router)
        {
            shardRouter = router ?? throw new ArgumentNullException(nameof(router));
        }
        public async Task<Guid?> GetCreatorIdAsync(int episodeId, Guid currentUserId)
        {
            var db = shardRouter.GetDbForUser(currentUserId);

            return await db.ScamflixEpisodes
                .Where(e => e.Id == episodeId)
                .Select(e => e.Creator.Id)
                .FirstOrDefaultAsync();
        }
    }
}
