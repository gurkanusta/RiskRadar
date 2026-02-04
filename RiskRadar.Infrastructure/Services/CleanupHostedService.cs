using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RiskRadar.Infrastructure.Persistence;

namespace RiskRadar.Infrastructure.Services;

public class CleanupHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public CleanupHostedService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var now = DateTime.UtcNow;

            
            var expiredBlocks = await db.BlockedIps
                .Where(x => x.BlockedUntilUtc <= now)
                .ToListAsync(stoppingToken);

            if (expiredBlocks.Count > 0)
                db.BlockedIps.RemoveRange(expiredBlocks);

            
            var expiredRefresh = await db.RefreshTokens
                .Where(x => x.ExpiresAtUtc <= now || x.RevokedAtUtc != null)
                .ToListAsync(stoppingToken);

            
            if (expiredRefresh.Count > 0)
                db.RefreshTokens.RemoveRange(expiredRefresh);

            if (expiredBlocks.Count > 0 || expiredRefresh.Count > 0)
                await db.SaveChangesAsync(stoppingToken);
        }
    }
}
