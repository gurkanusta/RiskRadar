namespace RiskRadar.Application.Services;

public static class RiskPolicy
{
    public const int FailLimit = 5;               
    public static readonly TimeSpan FailWindow = TimeSpan.FromMinutes(10); 
    public static readonly TimeSpan BanDuration = TimeSpan.FromMinutes(10); 
}
