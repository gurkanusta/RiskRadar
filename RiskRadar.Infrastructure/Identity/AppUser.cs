using Microsoft.AspNetCore.Identity;

namespace RiskRadar.Infrastructure.Identity;

public class AppUser : IdentityUser
{
    public bool IsDisabled { get; set; }
}
