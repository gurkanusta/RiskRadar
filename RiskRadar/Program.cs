using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RiskRadar.Api.Middlewares;
using RiskRadar.Application.Services;
using RiskRadar.Domain.Entities;
using RiskRadar.Infrastructure.Persistence;
using RiskRadar.Infrastructure.Security;
using System.Text;
using System.Threading.RateLimiting;
using RiskRadar.Infrastructure.Identity;
using RiskRadar.Infrastructure.Services;


using RiskRadar.Api.Features.Auth;
using RiskRadar.Api.Features.Admin;

using RiskRadar.Api.Features.Risk;





var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


builder.Services.AddIdentity<RiskRadar.Infrastructure.Identity.AppUser, IdentityRole>(opt =>
{
    opt.Password.RequireNonAlphanumeric = false;
    opt.Lockout.MaxFailedAccessAttempts = 5;
    opt.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();



builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<RiskScoringService>();


builder.Services.AddHostedService<RiskRadar.Infrastructure.Services.CleanupHostedService>();




var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key))
        };
    });

builder.Services.AddAuthorization();


builder.Services.AddRateLimiter(opt =>
{
    opt.AddFixedWindowLimiter("login", o =>
    {
        o.PermitLimit = 5; 
        o.Window = TimeSpan.FromMinutes(1);
        o.QueueLimit = 0;
        o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });

    opt.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();


app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseHttpsRedirection();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();


app.MapAuthEndpoints();
app.MapAdminEndpoints();
app.MapRiskEndpoints();


app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();




await SeedRolesAsync(app);
await SeedAdminAsync(app);

app.Run();


static async Task SeedRolesAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    foreach (var role in new[] { "User", "Admin" })
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
}

static async Task SeedAdminAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();

    var userManager = scope.ServiceProvider
        .GetRequiredService<UserManager<RiskRadar.Infrastructure.Identity.AppUser>>();

    var roleManager = scope.ServiceProvider
        .GetRequiredService<RoleManager<IdentityRole>>();

    const string adminEmail = "admin@riskradar.com";
    const string adminPass = "Admin1234!";

    
    if (!await roleManager.RoleExistsAsync("Admin"))
        await roleManager.CreateAsync(new IdentityRole("Admin"));

    
    var admin = await userManager.FindByEmailAsync(adminEmail);
    if (admin == null)
    {
        admin = new RiskRadar.Infrastructure.Identity.AppUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true
        };

        var createRes = await userManager.CreateAsync(admin, adminPass);
        if (!createRes.Succeeded)
            throw new Exception(string.Join("; ", createRes.Errors.Select(e => e.Description)));
    }
    else
    {
        
        await userManager.SetLockoutEndDateAsync(admin, null);
        await userManager.ResetAccessFailedCountAsync(admin);

        
        var resetToken = await userManager.GeneratePasswordResetTokenAsync(admin);
        var resetRes = await userManager.ResetPasswordAsync(admin, resetToken, adminPass);

        if (!resetRes.Succeeded)
        {
            
            throw new Exception(string.Join("; ", resetRes.Errors.Select(e => e.Description)));
        }

        
        admin.EmailConfirmed = true;
        await userManager.UpdateAsync(admin);
    }

    
    if (!await userManager.IsInRoleAsync(admin, "Admin"))
        await userManager.AddToRoleAsync(admin, "Admin");
}


