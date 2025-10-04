using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace DebtManager.Web.Auth;

// Maps Azure AD B2C scopes (scp claim) to role claims understood by [Authorize(Roles=...)]
public class B2CRoleClaimsTransformation : IClaimsTransformation
{
    private readonly IConfiguration _config;

    public B2CRoleClaimsTransformation(IConfiguration config)
    {
        _config = config;
    }

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity id || !id.IsAuthenticated)
            return Task.FromResult(principal);

        var scopes = _config.GetSection("AzureB2CScopes");
        var admin = scopes["Admin"];
        var client = scopes["Client"];
        var user = scopes["User"];

        var scpValues = id.FindAll("scp").Select(c => c.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);

        void EnsureRole(string roleName)
        {
            if (!id.HasClaim(c => c.Type == ClaimTypes.Role && c.Value == roleName))
            {
                id.AddClaim(new Claim(ClaimTypes.Role, roleName));
            }
        }

        if (!string.IsNullOrWhiteSpace(admin) && scpValues.Contains(admin)) EnsureRole("Admin");
        if (!string.IsNullOrWhiteSpace(client) && scpValues.Contains(client)) EnsureRole("Client");
        if (!string.IsNullOrWhiteSpace(user) && scpValues.Contains(user)) EnsureRole("User");

        return Task.FromResult(principal);
    }
}
