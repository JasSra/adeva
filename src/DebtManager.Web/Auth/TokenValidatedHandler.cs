using System.Security.Claims;
using DebtManager.Domain.Debtors;
using DebtManager.Domain.Organizations;
using DebtManager.Infrastructure.Identity;
using DebtManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DebtManager.Web.Auth;

public static class TokenValidatedHandler
{
    public static async Task OnTokenValidated(TokenValidatedContext ctx)
    {
        var services = ctx.HttpContext.RequestServices;
        var db = services.GetRequiredService<AppDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
        var config = services.GetRequiredService<IConfiguration>();

        var oid = ctx.Principal?.FindFirstValue("oid") ?? ctx.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = ctx.Principal?.FindFirstValue(ClaimTypes.Email) ?? ctx.Principal?.FindFirstValue("emails");
        var name = ctx.Principal?.Identity?.Name ?? ctx.Principal?.FindFirstValue("name");

        if (string.IsNullOrWhiteSpace(oid)) return;

        var user = await userManager.Users.FirstOrDefaultAsync(u => u.ExternalAuthId == oid);
        if (user is null)
        {
            user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                ExternalAuthId = oid,
                Email = email,
                UserName = email ?? oid,
                EmailConfirmed = true,
            };
            await userManager.CreateAsync(user);
        }

        // Auto-admin if no admins exist yet
        var anyAdmins = await db.UserRoles
            .Join(db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur, r })
            .AnyAsync(x => x.r.Name == "Admin");
        if (!anyAdmins)
        {
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new ApplicationRole("Admin"));
            }
            if (!await userManager.IsInRoleAsync(user, "Admin"))
            {
                await userManager.AddToRoleAsync(user, "Admin");
            }
        }

        // Map scopes to roles and ensure membership
        var scopes = config.GetSection("AzureB2CScopes");
        var roleMap = new Dictionary<string, string?>
        {
            [scopes["Admin"] ?? string.Empty] = "Admin",
            [scopes["Client"] ?? string.Empty] = "Client",
            [scopes["User"] ?? string.Empty] = "User",
        };
        var scpClaims = ctx.Principal?.FindAll("scp").Select(c => c.Value).ToHashSet() ?? new HashSet<string>();
        foreach (var kvp in roleMap)
        {
            if (!string.IsNullOrEmpty(kvp.Key) && scpClaims.Contains(kvp.Key) && kvp.Value is string roleName)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new ApplicationRole(roleName));
                }
                if (!await userManager.IsInRoleAsync(user, roleName))
                {
                    await userManager.AddToRoleAsync(user, roleName);
                }
            }
        }

        // Ensure profile exists
        var profile = await db.UserProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
        if (profile == null)
        {
            profile = new UserProfile
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
            };
            db.UserProfiles.Add(profile);
            await db.SaveChangesAsync();
        }

        // Choose landing based on scopes
        var adminScope = scopes["Admin"] ?? string.Empty;
        var clientScope = scopes["Client"] ?? string.Empty;
        var userScope = scopes["User"] ?? string.Empty;
        string landing = "/";
        if (!string.IsNullOrEmpty(adminScope) && scpClaims.Contains(adminScope)) landing = "/Admin";
        else if (!string.IsNullOrEmpty(clientScope) && scpClaims.Contains(clientScope)) landing = "/Client";
        else if (!string.IsNullOrEmpty(userScope) && scpClaims.Contains(userScope)) landing = "/User";

        // Set the redirect uri after sign-in
        ctx.Properties.RedirectUri = landing;
    }
}
