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

        public DbSet<UserRequest> UserRequests { get; set; }

        public DbSet<UserWallet> UserWallets { get; set; }
        public DbSet<WalletTransaction> WalletTransactions { get; set; }

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

            // Configure UserRequest entity - VERIFIED AGAINST ENTITY
            modelBuilder.Entity<UserRequest>(entity =>
            {
                // Primary Key
                entity.HasKey(e => e.Id);

                // Required Guid foreign keys
                entity.Property(e => e.RequestorId)
                    .IsRequired();

                entity.Property(e => e.ProductId)
                    .IsRequired();

                entity.Property(e => e.CreatorId)
                    .IsRequired();

                // String properties with max lengths - CORRECTED TO MATCH ENTITY
                entity.Property(e => e.ProductName)
                    .IsRequired()
                    .HasMaxLength(50); // ✅ Matches entity [MaxLength(50)]

                entity.Property(e => e.BodyPart)
                    .IsRequired()
                    .HasMaxLength(50); // ✅ Matches entity [MaxLength(50)]

                entity.Property(e => e.Text)
                    .HasMaxLength(50); // ✅ Matches entity [MaxLength(50)]

                entity.Property(e => e.DeliveredImageUrl)
                    .HasMaxLength(1000); // ✅ Matches entity [MaxLength(1000)]

                // Decimal properties with precision
                entity.Property(e => e.PriceAmount)
                    .HasColumnType("decimal(10,2)")
                    .IsRequired();

                entity.Property(e => e.ExtraAmount)
                    .HasColumnType("decimal(10,2)");

                entity.Property(e => e.TotalAmount)
                    .HasColumnType("decimal(10,2)");

                entity.Property(e => e.Tax)
                    .HasColumnType("decimal(10,2)");

                // Enum configurations (EF Core will store as int by default)
                entity.Property(e => e.Status)
                    .HasConversion<int>()
                    .IsRequired();

                entity.Property(e => e.DeadLine) // ✅ Correct property name from entity
                    .HasConversion<int>()
                    .IsRequired();

                // DateTime properties - CORRECTED ORDER TO MATCH ENTITY
                entity.Property(e => e.ExpectedDeliveredDate);

                entity.Property(e => e.DeliveredDate);

                entity.Property(e => e.CreatedAt)
                    .IsRequired();

                entity.Property(e => e.ModifiedAt)
                    .IsRequired();

                // AI Validation flags (nullable booleans)
                entity.Property(e => e.BodyPartValidated);

                entity.Property(e => e.TextValidated);

                entity.Property(e => e.ManualValidated);

                // Navigation Properties Configuration
                // Requester relationship
                entity.HasOne(ur => ur.Requester)
                    .WithMany() // Assuming RequesterSocial doesn't have a collection back to UserRequests
                    .HasForeignKey(ur => ur.RequestorId)
                    .OnDelete(DeleteBehavior.Restrict) // Prevent cascade delete
                    .HasConstraintName("FK_UserRequest_RequesterSocial");

                // Creator relationship
                entity.HasOne(ur => ur.Creator)
                    .WithMany() // Assuming CreatorSocial doesn't have a collection back to UserRequests
                    .HasForeignKey(ur => ur.CreatorId)
                    .OnDelete(DeleteBehavior.Restrict) // Prevent cascade delete
                    .HasConstraintName("FK_UserRequest_CreatorSocial");

                // Indexes for performance
                entity.HasIndex(e => e.RequestorId)
                    .HasDatabaseName("IX_UserRequests_RequestorId");

                entity.HasIndex(e => e.CreatorId)
                    .HasDatabaseName("IX_UserRequests_CreatorId");

                entity.HasIndex(e => e.ProductId)
                    .HasDatabaseName("IX_UserRequests_ProductId");

                entity.HasIndex(e => e.Status)
                    .HasDatabaseName("IX_UserRequests_Status");

                entity.HasIndex(e => e.CreatedAt)
                    .HasDatabaseName("IX_UserRequests_CreatedAt");

                entity.HasIndex(e => e.DeadLine) // ✅ Correct property name
                    .HasDatabaseName("IX_UserRequests_DeadLine"); // ✅ Updated index name

                entity.HasIndex(e => e.ExpectedDeliveredDate)
                    .HasDatabaseName("IX_UserRequests_ExpectedDeliveredDate");

                entity.HasIndex(e => e.DeliveredDate)
                    .HasDatabaseName("IX_UserRequests_DeliveredDate");

                // Composite indexes for common queries
                entity.HasIndex(e => new { e.Status, e.CreatedAt })
                    .HasDatabaseName("IX_UserRequests_Status_CreatedAt");

                entity.HasIndex(e => new { e.CreatorId, e.Status })
                    .HasDatabaseName("IX_UserRequests_CreatorId_Status");

                entity.HasIndex(e => new { e.RequestorId, e.Status })
                    .HasDatabaseName("IX_UserRequests_RequestorId_Status");

                entity.HasIndex(e => new { e.DeadLine, e.ExpectedDeliveredDate })
                    .HasDatabaseName("IX_UserRequests_DeadLine_ExpectedDate");

                // AI Validation indexes
                entity.HasIndex(e => e.BodyPartValidated)
                    .HasDatabaseName("IX_UserRequests_BodyPartValidated");

                entity.HasIndex(e => e.TextValidated)
                    .HasDatabaseName("IX_UserRequests_TextValidated");

                entity.HasIndex(e => e.ManualValidated)
                    .HasDatabaseName("IX_UserRequests_ManualValidated");

                // Composite index for validation status queries
                entity.HasIndex(e => new { e.BodyPartValidated, e.TextValidated, e.ManualValidated })
                    .HasDatabaseName("IX_UserRequests_ValidationStatus");

                // Index for delivered content queries
                entity.HasIndex(e => new { e.Status, e.DeliveredDate })
                    .HasDatabaseName("IX_UserRequests_Status_DeliveredDate");
            });

            // Configure UserWallet entity
            modelBuilder.Entity<UserWallet>(entity =>
            {
                entity.HasKey(e => e.UserId);

                entity.Property(e => e.GemBalance)
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();

                entity.Property(e => e.UsdBalance)
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();

                entity.Property(e => e.LastUpdated)
                    .IsRequired();

                entity.Property(e => e.CreatedAt)
                    .IsRequired();

                // Indexes
                entity.HasIndex(e => e.GemBalance)
                    .HasDatabaseName("IX_UserWallets_GemBalance");

                entity.HasIndex(e => e.UsdBalance)
                    .HasDatabaseName("IX_UserWallets_UsdBalance");

                entity.HasIndex(e => e.LastUpdated)
                    .HasDatabaseName("IX_UserWallets_LastUpdated");
            });

            // Configure WalletTransaction entity
            modelBuilder.Entity<WalletTransaction>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.UserId)
                    .IsRequired();

                entity.Property(e => e.Type)
                    .HasConversion<int>()
                    .IsRequired();

                entity.Property(e => e.Amount)
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();

                entity.Property(e => e.Currency)
                    .IsRequired()
                    .HasMaxLength(10);

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.RelatedEntityType)
                    .HasMaxLength(50);

                entity.Property(e => e.CreatedAt)
                    .IsRequired();

                // Indexes
                entity.HasIndex(e => e.UserId)
                    .HasDatabaseName("IX_WalletTransactions_UserId");

                entity.HasIndex(e => e.Type)
                    .HasDatabaseName("IX_WalletTransactions_Type");

                entity.HasIndex(e => e.Currency)
                    .HasDatabaseName("IX_WalletTransactions_Currency");

                entity.HasIndex(e => e.CreatedAt)
                    .HasDatabaseName("IX_WalletTransactions_CreatedAt");

                entity.HasIndex(e => e.RelatedEntityId)
                    .HasDatabaseName("IX_WalletTransactions_RelatedEntityId");

                // Composite indexes
                entity.HasIndex(e => new { e.UserId, e.CreatedAt })
                    .HasDatabaseName("IX_WalletTransactions_UserId_CreatedAt");

                entity.HasIndex(e => new { e.UserId, e.Currency })
                    .HasDatabaseName("IX_WalletTransactions_UserId_Currency");

                entity.HasIndex(e => new { e.RelatedEntityId, e.RelatedEntityType })
                    .HasDatabaseName("IX_WalletTransactions_RelatedEntity");
            });

        }

    }
}
