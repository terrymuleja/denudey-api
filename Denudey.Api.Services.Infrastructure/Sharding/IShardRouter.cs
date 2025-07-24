using Denudey.Api.Services.Infrastructure.Sharding;
using Denudey.Api.Services.Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;

public interface IShardRouter
{
    ApplicationDbContext GetDbForUser(Guid userId);
}