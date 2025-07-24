using Denudey.Api.Services.Infrastructure;
using Denudey.Api.Services.Infrastructure.DbContexts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;

namespace DenudeyApi.Services;

public class TokenCleanupService(IServiceScopeFactory scopeFactory, ILogger<TokenCleanupService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            try
            {
                var now = DateTime.UtcNow;

                var expired = await db.RefreshTokens
                    .Where(t => t.ExpiresAt < now && t.Revoked == null)
                    .ToListAsync(stoppingToken);

                foreach (var token in expired)
                    token.Revoked = DateTime.UtcNow;

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
