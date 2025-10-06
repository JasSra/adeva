using System.Security.Claims;
using DebtManager.Infrastructure.Identity;
using DebtManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DebtManager.Web.Middleware;

public class SecurityEnforcementMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityEnforcementMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context, AppDbContext db)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (ShouldBypass(path))
        {
            await _next(context);
            return;
        }

        if (!(context.User?.Identity?.IsAuthenticated ?? false))
        {
            await _next(context);
            return;
        }

        var uid = context.User.FindFirstValue("oid") ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (uid is null)
        {
            await _next(context);
            return;
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.ExternalAuthId == uid);
        if (user == null)
        {
            await _next(context);
            return;
        }

        var profile = await db.UserProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == user.Id);

        // Allow entire security setup flow through
        var atSecurityFlow = IsSecuritySetupFlow(path);

        // Enforce: Admins must have TOTP; Clients/Users must have confirmed phone
        var isAdmin = context.User.IsInRole("Admin");
        var needsSecuritySetup = isAdmin ? !user.TwoFactorEnabled : !user.PhoneNumberConfirmed;

        if (needsSecuritySetup && !atSecurityFlow)
        {
            context.Response.Redirect("/Security/Setup");
            return;
        }

        if (atSecurityFlow)
        {
            await _next(context);
            return;
        }

        // Client org required for client role
        if (context.User.IsInRole("Client"))
        {
            if (profile?.OrganizationId == null)
            {
                if (!path.StartsWith("/Client/Onboarding", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.Redirect("/Client/Onboarding");
                    return;
                }
            }
        }

        // Debtor profile required for user role
        if (context.User.IsInRole("User") && profile?.DebtorId == null)
        {
            if (!path.StartsWith("/User/Onboarding", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.Redirect("/User/Onboarding");
                return;
            }
        }

        await _next(context);
    }

    private static bool ShouldBypass(string path)
    {
        if (string.IsNullOrEmpty(path)) return false;
        return path.StartsWith("/css/") || path.StartsWith("/js/") || path.StartsWith("/images/") || path.StartsWith("/health/") || path.StartsWith("/api/") || path.StartsWith("/Account/") || path.StartsWith("/Dev/") || path.StartsWith("/Article/");
    }

    private static bool IsSecuritySetupFlow(string path)
    {
        if (string.IsNullOrEmpty(path)) return false;
        return path.StartsWith("/Security/Setup", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/Security/SendSms", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/Security/Complete", StringComparison.OrdinalIgnoreCase);
    }
}
