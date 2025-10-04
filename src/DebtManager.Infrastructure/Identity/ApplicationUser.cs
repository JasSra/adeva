using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace DebtManager.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    // External identity provider subject/object id (e.g., Azure AD B2C oid)
    public string? ExternalAuthId { get; set; }

    // TOTP/2FA
    public bool TotpEnabled { get; set; }
    public string? TotpSecretKey { get; set; }
    public string? TotpRecoveryCodes { get; set; }

    // 1-1 Profile (optional)
    public UserProfile? Profile { get; set; }
}
