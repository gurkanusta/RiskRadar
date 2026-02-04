using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

using RiskRadar.Infrastructure.Services;


namespace RiskRadar.Api.Features.Risk;

public static class RiskEndpoints
{
    public static IEndpointRouteBuilder MapRiskEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/risk").WithTags("Risk");


        
        group.MapGet("/score", async (string ip, RiskScoringService svc) =>
        {
            var result = await svc.ScoreIpAsync(ip);
            return Results.Ok(result);


        });



        group.MapGet("/my-ip-score", async (HttpContext ctx, RiskScoringService svc) =>
        {
            var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var result = await svc.ScoreIpAsync(ip);
            return Results.Ok(result);
        });





        group.MapGet("/top-ips",
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        async (RiskRadar.Infrastructure.Persistence.AppDbContext db) =>
    {
        var since = DateTime.UtcNow.AddHours(-24);

        var top = await db.RiskEvents
            .AsNoTracking()
            .Where(x => x.CreatedAtUtc >= since)
            .GroupBy(x => x.Ip)
            .Select(g => new
            {
                Ip = g.Key,
                Events = g.Count(),
                TotalScoreDelta = g.Sum(x => x.ScoreDelta),
                LastSeenUtc = g.Max(x => x.CreatedAtUtc)
            })
            .OrderByDescending(x => x.TotalScoreDelta)
            .ThenByDescending(x => x.Events)
            .Take(20)
            .ToListAsync();

        return Results.Ok(top);
    });



        group.MapGet("/events",
            [Authorize(Roles = "Admin")]
        async (RiskRadar.Infrastructure.Persistence.AppDbContext db) =>
            {
                var list = db.RiskEvents
                    .AsNoTracking()
                    .OrderByDescending(x => x.CreatedAtUtc)
                    .Select(x => new { x.Ip, x.Type, x.ScoreDelta, x.Details, x.CreatedAtUtc })
                    .Take(50)
                    .ToList();

                return Results.Ok(list);
            });

        return app;
    }
}
