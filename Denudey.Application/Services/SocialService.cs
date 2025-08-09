using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Denudey.Api.Domain.Entities;
using Denudey.Api.Services.Infrastructure.DbContexts;
using Denudey.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Denudey.Application.Services
{
    public class SocialService : ISocialService
    {
        private readonly StatsDbContext _statsDbContext; // Separate stats database
        private readonly ILogger<SocialService> _logger;

        public SocialService(StatsDbContext statsDbContext, ILogger<SocialService> logger)
        {
            _statsDbContext = statsDbContext;
            _logger = logger;
        }

        public async Task UpdateUserSocialProfileAsync(ApplicationUser user, string role, CancellationToken cancellationToken = default)
        {
            var normalizedRole = role?.ToLower() ?? "requester";

            try
            {
                switch (normalizedRole)
                {
                    case "requester":
                        await UpdateRequesterSocialAsync(user, cancellationToken);
                        break;
                    case "model":
                        await UpdateCreatorSocialAsync(user, cancellationToken);
                        break;
                    default:
                        _logger.LogWarning("Unknown role {Role} for user {UserId}", role, user.Id);
                        break;
                }

                await _statsDbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Updated social profile in statsDb for user {UserId} with role {Role}", user.Id, normalizedRole);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating social profile in statsDb for user {UserId} with role {Role}", user.Id, role);
                throw;
            }
        }

        public async Task<object?> GetUserSocialProfileAsync(Guid userId, string role, CancellationToken cancellationToken = default)
        {
            var normalizedRole = role?.ToLower() ?? "requester";

            return normalizedRole switch
            {
                "requester" => await _statsDbContext.RequesterSocials
                    .FirstOrDefaultAsync(rs => rs.RequesterId == userId, cancellationToken),
                "model" => await _statsDbContext.CreatorSocials
                    .FirstOrDefaultAsync(cs => cs.CreatorId == userId, cancellationToken),
                _ => null
            };
        }

        public async Task RemoveOtherSocialProfilesAsync(Guid userId, string excludeRole, CancellationToken cancellationToken = default)
        {
            var normalizedExcludeRole = excludeRole?.ToLower();

            try
            {
                if (normalizedExcludeRole != "requester")
                {
                    var requesterSocial = await _statsDbContext.RequesterSocials
                        .FirstOrDefaultAsync(rs => rs.RequesterId == userId, cancellationToken);
                    if (requesterSocial != null)
                    {
                        _statsDbContext.RequesterSocials.Remove(requesterSocial);
                    }
                }

                if (normalizedExcludeRole != "model")
                {
                    var creatorSocial = await _statsDbContext.CreatorSocials
                        .FirstOrDefaultAsync(cs => cs.CreatorId == userId, cancellationToken);
                    if (creatorSocial != null)
                    {
                        _statsDbContext.CreatorSocials.Remove(creatorSocial);
                    }
                }

                await _statsDbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Removed other social profiles from statsDb for user {UserId}, keeping {Role}", userId, excludeRole);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing other social profiles from statsDb for user {UserId}", userId);
                throw;
            }
        }

        public async Task RemoveAllSocialProfilesAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var requesterSocial = await _statsDbContext.RequesterSocials
                    .FirstOrDefaultAsync(rs => rs.RequesterId == userId, cancellationToken);
                if (requesterSocial != null)
                {
                    _statsDbContext.RequesterSocials.Remove(requesterSocial);
                }

                var creatorSocial = await _statsDbContext.CreatorSocials
                    .FirstOrDefaultAsync(cs => cs.CreatorId == userId, cancellationToken);
                if (creatorSocial != null)
                {
                    _statsDbContext.CreatorSocials.Remove(creatorSocial);
                }

                await _statsDbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Removed all social profiles from statsDb for user {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing all social profiles from statsDb for user {UserId}", userId);
                throw;
            }
        }

        private async Task UpdateRequesterSocialAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            var requesterSocial = await _statsDbContext.RequesterSocials
                .FirstOrDefaultAsync(rs => rs.RequesterId == user.Id, cancellationToken);

            if (requesterSocial == null)
            {
                requesterSocial = new RequesterSocial
                {
                    RequesterId = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    CountryCode = user.CountryCode,
                    Phone = user.Phone,
                    ProfileImageUrl = user.ProfileImageUrl,
                    IsPrivate = user.IsPrivate,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _statsDbContext.RequesterSocials.Add(requesterSocial);
                _logger.LogDebug("Created new RequesterSocial in statsDb for user {UserId}", user.Id);
            }
            else
            {
                // Update existing record with latest data from main DB
                requesterSocial.Username = user.Username;
                requesterSocial.Email = user.Email;
                requesterSocial.CountryCode = user.CountryCode;
                requesterSocial.Phone = user.Phone;
                requesterSocial.ProfileImageUrl = user.ProfileImageUrl;
                requesterSocial.IsPrivate = user.IsPrivate;
                requesterSocial.UpdatedAt = DateTime.UtcNow;
                _logger.LogDebug("Updated existing RequesterSocial in statsDb for user {UserId}", user.Id);
            }
        }

        private async Task UpdateCreatorSocialAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            var creatorSocial = await _statsDbContext.CreatorSocials
                .FirstOrDefaultAsync(cs => cs.CreatorId == user.Id, cancellationToken);

            if (creatorSocial == null)
            {
                creatorSocial = new CreatorSocial
                {
                    CreatorId = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    CountryCode = user.CountryCode,
                    Phone = user.Phone,
                    ProfileImageUrl = user.ProfileImageUrl,
                    IsPrivate = user.IsPrivate,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _statsDbContext.CreatorSocials.Add(creatorSocial);
                _logger.LogDebug("Created new CreatorSocial in statsDb for user {UserId}", user.Id);
            }
            else
            {
                // Update existing record with latest data from main DB
                creatorSocial.Username = user.Username;
                creatorSocial.Email = user.Email;
                creatorSocial.CountryCode = user.CountryCode;
                creatorSocial.Phone = user.Phone;
                creatorSocial.ProfileImageUrl = user.ProfileImageUrl;
                creatorSocial.IsPrivate = user.IsPrivate;
                creatorSocial.UpdatedAt = DateTime.UtcNow;
                _logger.LogDebug("Updated existing CreatorSocial in statsDb for user {UserId}", user.Id);
            }
        }
    }
}
