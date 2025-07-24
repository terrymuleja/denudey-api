using System;
using Denudey.Api.Services.Infrastructure.DbContexts;
using Denudey.Api.Services.Infrastructure;

namespace Denudey.Api.Services.Infrastructure.Sharding
{
    public interface IShardRouter
    {
        ApplicationDbContext GetDbForUser(Guid userId);
    }
}
