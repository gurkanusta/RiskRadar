using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RiskRadar.Infrastructure.Persistence;

namespace RiskRadar.Api.Features.Admin;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin")
            .WithTags("Admin")
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });

        group.MapGet("/blocked-ips", async (AppDbContext db) =>
        {
            var list = await db.BlockedIps
                .AsNoTracking()
                .OrderByDescending(x => x.BlockedUntilUtc)
                .Select(x => new { x.Ip, x.BlockedUntilUtc, x.Reason, x.CreatedAtUtc })
                .ToListAsync();

            return Results.Ok(list);
        });

        group.MapPost("/unblock-ip/{ip}", async (string ip, AppDbContext db) =>
        {
            var item = await db.BlockedIps.FirstOrDefaultAsync(x => x.Ip == ip);
            if (item == null) return Results.NotFound();

            db.BlockedIps.Remove(item);
            await db.SaveChangesAsync();

            return Results.Ok(new { message = "Unblocked.", ip });
        });

        group.MapPost("/promote/{email}", async (string email, UserManager<RiskRadar.Infrastructure.Identity.AppUser> userManager) =>
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null) return Results.NotFound(new { error = "User not found." });

            if (!await userManager.IsInRoleAsync(user, "Admin"))
                await userManager.AddToRoleAsync(user, "Admin");

            return Results.Ok(new { message = "User promoted to Admin.", email });
        });

        return app;
    }
}
