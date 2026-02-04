using Microsoft.AspNetCore.RateLimiting;
using RiskRadar.Application.Contracts;
using RiskRadar.Application.Services;

namespace RiskRadar.Api.Features.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/register", async (RegisterRequest req, IAuthService svc) =>
        {
            await svc.RegisterAsync(req);
            return Results.Ok(new { message = "Registered." });
        });

        group.MapPost("/login",
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("login")]
        async (HttpContext ctx, LoginRequest req, RiskRadar.Application.Services.IAuthService svc) =>
    {
        var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var ua = ctx.Request.Headers.UserAgent.ToString();
        var cid = ctx.Items[RiskRadar.Api.Middlewares.CorrelationIdMiddleware.HeaderName]?.ToString();

        
        var res = await svc.LoginAsync(req, ip, ua);
        return Results.Ok(res);
    });





        group.MapPost("/logout", async (LogoutRequest req, IAuthService svc) =>
        {
            await svc.LogoutAsync(req);
            return Results.Ok(new { message = "Logged out." });
        });


        group.MapPost("/refresh", async (RefreshRequest req, IAuthService svc) =>
        {
            var res = await svc.RefreshAsync(req);
            return Results.Ok(res);
        });

        return app;
    }
}
