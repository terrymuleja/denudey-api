using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Denudey.Api.Services.Infrastructure.Sharding;
using Denudey.Api.Services.Infrastructure;
using Denudey.Api.Services.Infrastructure.DbContexts;

namespace Denudey.Api.Services.Infrastructure.Sharding.Implementations
{
    /// <summary>
    /// Later, replace SingleShardRouter with MultiShardRouter.
    /// </summary>
    /// <param name="db"></param>
    public class SingleShardRouter (ApplicationDbContext db) : IShardRouter
    {
        public ApplicationDbContext GetDbForUser(Guid userId) => db;
    }
}
