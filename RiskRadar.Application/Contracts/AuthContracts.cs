namespace RiskRadar.Application.Contracts;

public record RegisterRequest(string Email, string Password);
public record LoginRequest(string Email, string Password);
public record RefreshRequest(string RefreshToken);

public record AuthResult(string AccessToken, string RefreshToken);
