namespace RiskRadar.Application.Contracts;

public record RiskScoreResult(
    string Ip,
    int Score,
    bool IsBlocked,
    DateTime? BlockedUntilUtc,
    int FailedLoginsLast10m,
    int DistinctEmailsLast10m,
    bool UserAgentChangedLast10m,
    IReadOnlyList<string> Reasons
);
