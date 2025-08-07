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

        public DbSet<ProductView> ProductViews => Set<ProductView>();

        public DbSet<ProductLike> ProductLikes => Set<ProductLike>();


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<EpisodeLike>()
                .HasKey(el => new { el.UserId, el.EpisodeId }); // Composite PK

            modelBuilder.Entity<EpisodeView>()
                .HasKey(ev => ev.Id);

            modelBuilder.Entity<ProductLike>()
                .HasKey(el => new { el.UserId, el.ProductId }); // Composite PK

            modelBuilder.Entity<ProductView>()
                .HasKey(ev => ev.Id);
        }

    }


}
