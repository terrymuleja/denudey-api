using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CloudinaryDotNet.Core;
using Denudey.Api.Domain.DTOs;
using Denudey.Api.Domain.Entities;
using Denudey.Api.Services.Cloudinary.Interfaces;
using Denudey.Api.Services.Infrastructure.DbContexts;
using Denudey.Application.Interfaces;
using Elastic.Clients.Elasticsearch.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Denudey.Application.Services
{
    public class ProfileService : IProfileService
    {
        private readonly ISocialService _socialService; // Handles statsDb operations
        private readonly ICloudinaryService _cloudinaryService;
        private readonly ILogger<ProfileService> _logger;
        private readonly IShardRouter _shardRouter;

        public ProfileService(
            ApplicationDbContext dbContext,
            ISocialService socialService,
            ICloudinaryService cloudinaryService,
                IShardRouter shardRouter,
            ILogger<ProfileService> logger)
        {
            _socialService = socialService;
            _cloudinaryService = cloudinaryService;
            _logger = logger;
            _shardRouter = shardRouter;
        }

        public async Task<MeResponse?> GetUserProfileAsync(Guid userId, string? accessToken, CancellationToken cancellationToken = default)
        {
            var db = _shardRouter.GetDbForUser(userId);
            var user = await db.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user == null) return null;

            var refreshToken = await db.RefreshTokens
                .Where(rt => rt.UserId == user.Id && rt.Token == accessToken)
                .OrderByDescending(rt => rt.ExpiresAt)
                .FirstOrDefaultAsync();

            var role = user.UserRoles
                .Select(ur => ur.Role.Name)
                .FirstOrDefault() ?? "requester";

            return new MeResponse(
                user.Id,
                user.Username,
                role,
                user.Email,
                user.DeviceId,
                refreshToken?.ExpiresAt,
                user.UserRoles.Select(ur => ur.Role.Name).ToList(),
                CountryCode: user.CountryCode,
                Telephone: user.Phone,
                ProfileImageUrl: user.ProfileImageUrl,
                IsPrivate: user.IsPrivate
            );
        }

        public async Task UpdateProfileAsync(Guid userId, UpdateProfileDto dto, CancellationToken cancellationToken = default)
        {
            var db = _shardRouter.GetDbForUser(userId);

            var user = await GetUserWithRolesAsync(db, userId, cancellationToken);
            if (user == null)
                throw new InvalidOperationException("User not found");

            // Update main database
            user.CountryCode = dto.CountryCode;
            user.Phone = dto.Phone;
            user.ProfileImageUrl = dto.ProfileImageUrl;

            await db.SaveChangesAsync(cancellationToken);

            // Sync to stats database
            var role = user.UserRoles.FirstOrDefault()?.Role.Name ?? "requester";
            await _socialService.UpdateUserSocialProfileAsync(user, role, cancellationToken);

            _logger.LogInformation("Updated profile for user {UserId} and synced to statsDb", userId);
        }

        public async Task<bool> DeleteProfileImageAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var db = _shardRouter.GetDbForUser(userId);
            var user = await GetUserWithRolesAsync(db, userId, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found for profile image deletion", userId);
                return false;
            }

            // If user has no profile image, consider deletion successful
            if (string.IsNullOrEmpty(user.ProfileImageUrl))
            {
                _logger.LogInformation("User {UserId} has no profile image to delete, operation considered successful", userId);
                return true;
            }

            _logger.LogInformation("Attempting to delete profile image for user {UserId}: {ImageUrl}", userId, user.ProfileImageUrl);

            var deleted = await _cloudinaryService.DeleteImageFromCloudinary(user.ProfileImageUrl);

            if (deleted)
            {
                var oldImageUrl = user.ProfileImageUrl;
                user.ProfileImageUrl = null;
                await db.SaveChangesAsync(cancellationToken);

                // Sync to stats database
                var role = user.UserRoles.FirstOrDefault()?.Role.Name ?? "requester";
                await _socialService.UpdateUserSocialProfileAsync(user, role, cancellationToken);

                _logger.LogInformation("Successfully deleted profile image {ImageUrl} for user {UserId} and synced to statsDb", oldImageUrl, userId);
                return true;
            }
            else
            {
                _logger.LogWarning("Failed to delete profile image from Cloudinary for user {UserId}: {ImageUrl}", userId, user.ProfileImageUrl);
                return false;
            }
        }
        public async Task UpdatePrivacyAsync(Guid userId, bool isPrivate, CancellationToken cancellationToken = default)
        {
            var db = _shardRouter.GetDbForUser(userId);
            var user = await GetUserWithRolesAsync(db, userId, cancellationToken);
            if (user == null)
                throw new InvalidOperationException("User not found");

            user.IsPrivate = isPrivate;
            await db.SaveChangesAsync(cancellationToken);

            // Sync to stats database
            var role = user.UserRoles.FirstOrDefault()?.Role.Name ?? "requester";
            await _socialService.UpdateUserSocialProfileAsync(user, role, cancellationToken);

            _logger.LogInformation("Updated privacy setting for user {UserId} to {IsPrivate} and synced to statsDb", userId, isPrivate);
        }

        public async Task UpdateRoleAsync(Guid userId, string role, CancellationToken cancellationToken = default)
        {
            var db = _shardRouter.GetDbForUser(userId);
            var normalizedRole = role?.ToLower();
            if (normalizedRole != "model" && normalizedRole != "requester")
                throw new ArgumentException("Role must be 'Model' or 'Requester'.", nameof(role));

            // Use transaction only for main database
            using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var user = await GetUserWithRolesAsync(db, userId, cancellationToken);
                if (user == null)
                    throw new InvalidOperationException("User not found");

                // Get current role before changing
                var currentRole = user.UserRoles.FirstOrDefault()?.Role.Name?.ToLower();

                // Remove all existing roles in main DB
                db.UserRoles.RemoveRange(user.UserRoles);

                // Check if the role already exists
                var existingRole = await db.Roles
                    .FirstOrDefaultAsync(r => r.Name.ToLower() == normalizedRole, cancellationToken);

                if (existingRole == null)
                    throw new InvalidOperationException($"Role '{role}' not found");

                // Add new role in main DB
                user.UserRoles.Add(new UserRole
                {
                    UserId = userId,
                    Role = existingRole
                });

                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                // Now handle stats database updates (outside transaction since it's a different DB)
                // Remove old social profiles if role changed
                if (currentRole != null && currentRole != normalizedRole)
                {
                    await _socialService.RemoveOtherSocialProfilesAsync(userId, normalizedRole, cancellationToken);
                }

                // Update social profile for new role
                await _socialService.UpdateUserSocialProfileAsync(user, normalizedRole, cancellationToken);

                _logger.LogInformation("Updated role for user {UserId} from {OldRole} to {NewRole} and synced to statsDb",
                    userId, currentRole ?? "none", normalizedRole);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to update role for user {UserId} to {Role}", userId, role);
                throw;
            }
        }

        public async Task SyncUserToSocialProfileAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var db = _shardRouter.GetDbForUser(userId);
            var user = await GetUserWithRolesAsync(db, userId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("Cannot sync user {UserId} to social profile - user not found", userId);
                return;
            }

            var role = user.UserRoles.FirstOrDefault()?.Role.Name ?? "requester";
            await _socialService.UpdateUserSocialProfileAsync(user, role, cancellationToken);

            _logger.LogInformation("Synced user {UserId} to social profile in statsDb with role {Role}", userId, role);
        }

        private async Task<ApplicationUser?> GetUserWithRolesAsync(ApplicationDbContext db, Guid userId, CancellationToken cancellationToken)
        {
            
            return await db.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        }
    }
}
