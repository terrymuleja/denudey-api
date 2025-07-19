using System.Data;
using Denudey.Api.Domain.Entities;
using Denudey.Api.Domain.Models;
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
            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(u => u.Id);

                entity.Property(u => u.Username).IsRequired().HasMaxLength(256);
                entity.Property(u => u.Email).IsRequired().HasMaxLength(256);
                entity.Property(u => u.PasswordHash).IsRequired();
                entity.Property(u => u.CreatedAt).HasDefaultValueSql("NOW()");

            });
            

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
                entity.HasOne(e => e.Creator)
                    .WithMany(u => u.Episodes) // or .WithMany(u => u.Episodes) if you want reverse navigation
                    .HasForeignKey(e => e.CreatedBy)
                    .OnDelete(DeleteBehavior.Cascade);
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
