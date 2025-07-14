using Denudey.DataAccess;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;

namespace DenudeyApi.Services;

public class TokenCleanupService(ApplicationDbContext db, ILogger<TokenCleanupService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;

                // Revoke expired, still-active tokens
                var expired = await db.RefreshTokens
                    .Where(t => t.ExpiresAt < now && t.Revoked == null)
                    .ToListAsync(stoppingToken);

                foreach (var token in expired)
                    token.Revoked = DateTime.UtcNow;

                // Delete revoked tokens older than 30 days
                var oldRevoked = await db.RefreshTokens
                    .Where(t => t.Revoked != null && t.Revoked < now.AddDays(-30))
                    .ToListAsync(stoppingToken);

                if (expired.Any() || oldRevoked.Any())
                {
                    if (expired.Any())
                        logger.LogInformation($"[Cleanup] Revoking {expired.Count} expired tokens");

                    if (oldRevoked.Any())
                    {
                        db.RefreshTokens.RemoveRange(oldRevoked);
                        logger.LogInformation($"[Cleanup] Deleting {oldRevoked.Count} revoked tokens older than 30 days");
                    }

                    await db.SaveChangesAsync(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[Cleanup] Error cleaning up tokens");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
