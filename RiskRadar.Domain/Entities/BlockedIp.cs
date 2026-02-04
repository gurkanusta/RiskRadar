namespace RiskRadar.Domain.Entities;

public class BlockedIp
{
    public long Id { get; set; }
    public string Ip { get; set; } = default!;
    public DateTime BlockedUntilUtc { get; set; }
    public string Reason { get; set; } = "Too many failed login attempts";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
