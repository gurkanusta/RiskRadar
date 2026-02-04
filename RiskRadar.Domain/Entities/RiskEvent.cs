namespace RiskRadar.Domain.Entities;

public class RiskEvent
{
    public long Id { get; set; }

    public string Ip { get; set; } = default!;
    public string? Email { get; set; }
    public string? UserAgent { get; set; }

    public string Type { get; set; } = default!; 
    public int ScoreDelta { get; set; }          
    public string? Details { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
