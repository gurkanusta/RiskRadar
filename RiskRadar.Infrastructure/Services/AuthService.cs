using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RiskRadar.Application.Contracts;
using RiskRadar.Application.Services;
using RiskRadar.Domain.Entities;
using RiskRadar.Infrastructure.Identity;
using RiskRadar.Infrastructure.Persistence;
using RiskRadar.Infrastructure.Security;

namespace RiskRadar.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly AppDbContext _db;
    private readonly JwtService _jwt;

    public AuthService(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        AppDbContext db,
        JwtService jwt)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _db = db;
        _jwt = jwt;
    }

    public async Task RegisterAsync(RegisterRequest req)
    {
        var exists = await _userManager.FindByEmailAsync(req.Email);
        if (exists != null) throw new InvalidOperationException("Email already exists.");

        var user = new AppUser { UserName = req.Email, Email = req.Email };
        var res = await _userManager.CreateAsync(user, req.Password);
        if (!res.Succeeded)
            throw new InvalidOperationException(string.Join("; ", res.Errors.Select(e => e.Description)));

        await _userManager.AddToRoleAsync(user, "User");
    }

    public async Task<AuthResult> LoginAsync(LoginRequest req, string ip, string? userAgent)
    {
        
        var blocked = await _db.BlockedIps.AsNoTracking().FirstOrDefaultAsync(x => x.Ip == ip);
        if (blocked != null && blocked.BlockedUntilUtc > DateTime.UtcNow)
            throw new UnauthorizedAccessException($"IP blocked until {blocked.BlockedUntilUtc:O}");

        var user = await _userManager.FindByEmailAsync(req.Email);
        if (user == null)
        {
            await LogAttemptAsync(req.Email, ip, userAgent, false, "User not found");
            await ApplyBanIfNeededAsync(ip);
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        if (user.IsDisabled)
        {
            await LogAttemptAsync(req.Email, ip, userAgent, false, "User disabled");
            throw new UnauthorizedAccessException("User disabled.");
        }

        var signIn = await _signInManager.CheckPasswordSignInAsync(user, req.Password, lockoutOnFailure: true);
        if (!signIn.Succeeded)
        {
            await LogAttemptAsync(req.Email, ip, userAgent, false, "Wrong password");
            await ApplyBanIfNeededAsync(ip);
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        await LogAttemptAsync(req.Email, ip, userAgent, true, null);

        var access = await _jwt.CreateAccessTokenAsync(user);

        var refreshRaw = JwtService.GenerateRefreshTokenRaw();
        var refreshHash = JwtService.Sha256(refreshRaw);

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshHash,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7)
        });

        await _db.SaveChangesAsync();
        return new AuthResult(access, refreshRaw);
    }

    public async Task<AuthResult> RefreshAsync(RefreshRequest req)
    {
        var hash = JwtService.Sha256(req.RefreshToken);

        var token = await _db.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TokenHash == hash);

        if (token == null) throw new UnauthorizedAccessException("Invalid refresh token.");
        if (token.RevokedAtUtc != null) throw new UnauthorizedAccessException("Refresh token revoked.");
        if (token.ExpiresAtUtc < DateTime.UtcNow) throw new UnauthorizedAccessException("Refresh token expired.");

        
        var tracked = await _db.RefreshTokens.FirstAsync(x => x.Id == token.Id);
        tracked.RevokedAtUtc = DateTime.UtcNow;

        var newRaw = JwtService.GenerateRefreshTokenRaw();
        var newHash = JwtService.Sha256(newRaw);

        tracked.ReplacedByTokenHash = newHash;

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = tracked.UserId,
            TokenHash = newHash,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7)
        });

        await _db.SaveChangesAsync();

        var user = await _userManager.FindByIdAsync(tracked.UserId)
                   ?? throw new UnauthorizedAccessException("User not found.");

        var access = await _jwt.CreateAccessTokenAsync(user);
        return new AuthResult(access, newRaw);
    }

    private async Task LogAttemptAsync(string? email, string ip, string? userAgent, bool success, string? reason)
    {
        _db.LoginAttempts.Add(new LoginAttempt
        {
            Email = email,
            Ip = ip,
            UserAgent = userAgent,
            IsSuccess = success,
            FailureReason = reason
        });
        await _db.SaveChangesAsync();
    }

    private static class RiskPolicy
    {
        public const int FailLimit = 5;
        public static readonly TimeSpan FailWindow = TimeSpan.FromMinutes(10);
        public static readonly TimeSpan BanDuration = TimeSpan.FromMinutes(10);
    }

    private async Task ApplyBanIfNeededAsync(string ip)
    {
        var since = DateTime.UtcNow - RiskPolicy.FailWindow;
        var fails = await _db.LoginAttempts.AsNoTracking()
            .Where(x => x.Ip == ip && !x.IsSuccess && x.CreatedAtUtc >= since)
            .CountAsync();

        if (fails < RiskPolicy.FailLimit) return;

        var existing = await _db.BlockedIps.FirstOrDefaultAsync(x => x.Ip == ip);
        if (existing == null)
        {
            _db.BlockedIps.Add(new BlockedIp
            {
                Ip = ip,
                BlockedUntilUtc = DateTime.UtcNow.Add(RiskPolicy.BanDuration),
                Reason = $"Failed logins >= {RiskPolicy.FailLimit} in {RiskPolicy.FailWindow.TotalMinutes} minutes"
            });
        }
        else
        {
            existing.BlockedUntilUtc = DateTime.UtcNow.Add(RiskPolicy.BanDuration);
            existing.Reason = $"Failed logins >= {RiskPolicy.FailLimit} in {RiskPolicy.FailWindow.TotalMinutes} minutes";
        }

        await _db.SaveChangesAsync();
    }

    public async Task LogoutAsync(LogoutRequest req)
    {
        var hash = JwtService.Sha256(req.RefreshToken);
        var token = await _db.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == hash);
        if (token == null) return;

        token.RevokedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

}
