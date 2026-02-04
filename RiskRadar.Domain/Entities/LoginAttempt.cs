namespace RiskRadar.Domain.Entities;

public class LoginAttempt
{
    public long Id { get; set; }
    public string? Email { get; set; }
    public string Ip { get; set; } = default!;
    public string? UserAgent { get; set; }

    public bool IsSuccess { get; set; }
    public string? FailureReason { get; set; }

    public string? CorrelationId { get; set; }


    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
