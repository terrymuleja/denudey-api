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

        public DbSet<CreatorSocial> CreatorSocials { get; set; }
        public DbSet<RequesterSocial> RequesterSocials { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            modelBuilder.Entity<EpisodeLike>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.EpisodeId });

                entity.HasIndex(e => e.RequesterId);

                // Foreign key relationship
                entity.HasOne(e => e.Requester)
                    .WithMany(c => c.EpisodeLikes)
                    .HasForeignKey(e => e.RequesterId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<EpisodeView>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasIndex(e => e.RequesterId);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.EpisodeId);

                // Foreign key relationship
                entity.HasOne(e => e.Requester)
                    .WithMany(c => c.EpisodeViews)
                    .HasForeignKey(e => e.RequesterId)
                    .OnDelete(DeleteBehavior.Cascade);
            });


            modelBuilder.Entity<ProductLike>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.ProductId });

                entity.HasIndex(e => e.CreatorId);

                // Foreign key relationship
                entity.HasOne(e => e.Creator)
                    .WithMany(c => c.ProductLikes)
                    .HasForeignKey(e => e.CreatorId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ProductView>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasIndex(e => e.CreatorId);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.ProductId);

                // Foreign key relationship
                entity.HasOne(e => e.Creator)
                    .WithMany(c => c.ProductViews)
                    .HasForeignKey(e => e.CreatorId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<CreatorSocial>(entity =>
            {
                entity.HasKey(e => e.CreatorId);
                entity.Property(e => e.CreatorId).HasMaxLength(255);
                entity.Property(e => e.Username).HasMaxLength(255);
                entity.Property(e => e.ProfileImageUrl).HasColumnType("text");
                entity.Property(e => e.Bio).HasColumnType("text");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                // Indexes
                entity.HasIndex(e => e.Username).IsUnique();

                entity.HasIndex(e => e.UpdatedAt);
            });

            modelBuilder.Entity<RequesterSocial>(entity =>
            {
                entity.HasKey(e => e.RequesterId);
                entity.Property(e => e.RequesterId).HasMaxLength(255);
                entity.Property(e => e.Username).HasMaxLength(255);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            }); 
        }

    }


}
