using RiskRadar.Application.Contracts;

namespace RiskRadar.Application.Services;

public interface IAuthService
{
    Task RegisterAsync(RegisterRequest req);
    Task<AuthResult> LoginAsync(LoginRequest req, string ip, string? userAgent);
    Task<AuthResult> RefreshAsync(RefreshRequest req);


    Task LogoutAsync(LogoutRequest req);

}
