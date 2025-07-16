using System.Data;
using Denudey.Api.Domain.Entities;
using Denudey.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace Denudey.DataAccess
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : DbContext(options)
    {
        public DbSet<ApplicationUser> Users => Set<ApplicationUser>();

        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

        public DbSet<Role> Roles { get; set; }

        public DbSet<UserRole> UserRoles { get; set; }

        public DbSet<ScamflixEpisode> ScamflixEpisodes => Set<ScamflixEpisode>();


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Seed roles
            modelBuilder.Entity<Role>().HasData(
                new Role { Id = Guid.Parse("00000000-0000-0000-0000-000000000001"), Name = RoleNames.Admin },
                new Role { Id = Guid.Parse("00000000-0000-0000-0000-000000000002"), Name = RoleNames.Model },
                new Role { Id = Guid.Parse("00000000-0000-0000-0000-000000000003"), Name = RoleNames.Requester }
            );

            modelBuilder.Entity<UserRole>()
                .HasKey(ur => new { ur.UserId, ur.RoleId });

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId);

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId);

            modelBuilder.Entity<ScamflixEpisode>(entity =>
            {
                entity.ToTable("ScamflixEpisodes");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ImageUrl).IsRequired();
                entity.Property(e => e.Tags);
                entity.Property(e => e.CreatedBy).HasMaxLength(100);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            });

        }


    }
}
