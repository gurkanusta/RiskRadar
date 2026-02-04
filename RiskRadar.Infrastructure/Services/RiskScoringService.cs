using Microsoft.EntityFrameworkCore;
using RiskRadar.Application.Contracts;
using RiskRadar.Infrastructure.Persistence;

namespace RiskRadar.Infrastructure.Services;

public class RiskScoringService
{
    private readonly AppDbContext _db;

    public RiskScoringService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<RiskScoreResult> ScoreIpAsync(string ip)
    {
        var since = DateTime.UtcNow.AddMinutes(-10);

        var blocked = await _db.BlockedIps.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Ip == ip && x.BlockedUntilUtc > DateTime.UtcNow);

        var attempts = await _db.LoginAttempts.AsNoTracking()
            .Where(x => x.Ip == ip && x.CreatedAtUtc >= since)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync();

        int failed = attempts.Count(x => !x.IsSuccess);
        int distinctEmails = attempts
            .Where(x => !string.IsNullOrWhiteSpace(x.Email))
            .Select(x => x.Email!.ToLower())
            .Distinct()
            .Count();

        



        var uaList = attempts
            .Where(x => !string.IsNullOrWhiteSpace(x.UserAgent))
            .Select(x => x.UserAgent!)
            .Distinct()
            .ToList();

        bool uaChanged = uaList.Count >= 2;

        var reasons = new List<string>();
        int score = 0;

        
        if (blocked != null)
        {
            score += 60;
            reasons.Add("IP is currently blocked.");
            await AddEventAsync(ip, null, null, "BruteForce", 60, "IP blocked (BlockedIps table).");
        }

        
        if (failed >= 3)
        {
            var delta = Math.Min(30, failed * 6); 
            score += delta;
            reasons.Add($"Failed logins in last 10m: {failed}");
            await AddEventAsync(ip, null, null, "BruteForce", delta, $"Failed logins last 10m: {failed}");
        }

        
        if (distinctEmails >= 3)
        {
            score += 20;
            reasons.Add($"Many different emails tried: {distinctEmails}");
            await AddEventAsync(ip, null, null, "ManyEmails", 20, $"Distinct emails last 10m: {distinctEmails}");
        }

        
        if (uaChanged)
        {
            score += 15;
            reasons.Add("User-Agent changed within 10 minutes.");
            await AddEventAsync(ip, null, null, "SuspiciousUA", 15, $"Distinct UA count last 10m: {uaList.Count}");
        }

        




        score = Math.Max(0, Math.Min(100, score));

        
        if (reasons.Count == 0)
            reasons.Add("No suspicious activity detected in the last 10 minutes.");

        return new RiskScoreResult(
            Ip: ip,
            Score: score,
            IsBlocked: blocked != null,
            BlockedUntilUtc: blocked?.BlockedUntilUtc,
            FailedLoginsLast10m: failed,
            DistinctEmailsLast10m: distinctEmails,
            UserAgentChangedLast10m: uaChanged,
            Reasons: reasons
        );
    }

    private async Task AddEventAsync(string ip, string? email, string? ua, string type, int delta, string? details)
    {
        var since = DateTime.UtcNow.AddMinutes(-10);

        var exists = await _db.RiskEvents.AsNoTracking().AnyAsync(x =>
            x.Ip == ip && x.Type == type && x.CreatedAtUtc >= since);

        if (exists) return;

        _db.RiskEvents.Add(new RiskRadar.Domain.Entities.RiskEvent
        {
            Ip = ip,
            Email = email,
            UserAgent = ua,
            Type = type,
            ScoreDelta = delta,
            Details = details
        });

        await _db.SaveChangesAsync();
    }

}
