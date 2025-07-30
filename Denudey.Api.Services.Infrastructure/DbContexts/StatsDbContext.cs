using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Denudey.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Denudey.Api.Services.Infrastructure.DbContexts
{
    public class StatsDbContext(DbContextOptions<StatsDbContext> options) : DbContext(options)
    {
        public DbSet<EpisodeView> EpisodeViews => Set<EpisodeView>();
        public DbSet<EpisodeLike> EpisodeLikes => Set<EpisodeLike>();
    }
}
