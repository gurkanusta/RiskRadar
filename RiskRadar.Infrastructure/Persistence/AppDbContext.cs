using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RiskRadar.Domain.Entities;
using RiskRadar.Infrastructure.Identity;


namespace RiskRadar.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<AppUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<LoginAttempt> LoginAttempts => Set<LoginAttempt>();
    public DbSet<BlockedIp> BlockedIps => Set<BlockedIp>();
    public DbSet<RiskEvent> RiskEvents => Set<RiskEvent>();


    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<RefreshToken>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.TokenHash).IsUnique();
            e.Property(x => x.TokenHash).HasMaxLength(256);
            e.Property(x => x.UserId).IsRequired();
        });

        builder.Entity<LoginAttempt>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.Ip, x.CreatedAtUtc });
            e.Property(x => x.Ip).HasMaxLength(64).IsRequired();
            e.Property(x => x.Email).HasMaxLength(256);

            e.Property(x => x.CorrelationId).HasMaxLength(64);

            e.Property(x => x.UserAgent).HasMaxLength(512);
        });

        builder.Entity<BlockedIp>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Ip).IsUnique();
            e.Property(x => x.Ip).HasMaxLength(64).IsRequired();
        });

        builder.Entity<RiskEvent>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.Ip, x.CreatedAtUtc });
            e.Property(x => x.Ip).HasMaxLength(64).IsRequired();
            e.Property(x => x.Email).HasMaxLength(256);
            e.Property(x => x.UserAgent).HasMaxLength(512);
            e.Property(x => x.Type).HasMaxLength(64).IsRequired();
        });
    }
}
