using System.Security.Claims;
using DebtManager.Contracts.Configuration;

namespace DebtManager.Web.Middleware;

public class BootstrapNudgeMiddleware
{
    private readonly RequestDelegate _next;

    public BootstrapNudgeMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context, IAppConfigService cfg, IHostEnvironment env)
    {
        // Only nudge authenticated admins and only in non-static paths
        var path = context.Request.Path.Value ?? string.Empty;
        if (ShouldBypass(path))
        {
            await _next(context);
            return;
        }

        // Ignore if not signed in or not admin
        var user = context.User;
        var isAdmin = user?.Identity?.IsAuthenticated == true && (user.IsInRole("Admin") || user.HasClaim(c => c.Type == ClaimTypes.Role && c.Value == "Admin"));
        if (!isAdmin)
        {
            await _next(context);
            return;
        }

        // Sentinel bypass
        var sentinel = (await cfg.GetAsync("System:BootstrapComplete"))?.Trim().ToLowerInvariant();
        if (sentinel == "true")
        {
            await _next(context);
            return;
        }

        // Check required keys per env
        var required = new List<string>();
        if (env.IsProduction() || string.Equals(env.EnvironmentName, "Staging", StringComparison.OrdinalIgnoreCase))
        {
            required.AddRange(new[] { "Stripe:SecretKey", "Stripe:WebhookSecret" });
        }
        var missing = new List<string>();
        foreach (var k in required)
        {
            if (string.IsNullOrWhiteSpace(await cfg.GetAsync(k))) missing.Add(k);
        }

        if (missing.Count > 0 && !path.StartsWith("/Admin/Configuration/Secrets", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.Redirect("/Admin/Configuration/Secrets");
            return;
        }

        await _next(context);
    }

    private static bool ShouldBypass(string path)
    {
        if (string.IsNullOrEmpty(path)) return false;
        return path.StartsWith("/css/") || path.StartsWith("/js/") || path.StartsWith("/images/") || path.StartsWith("/health/") || path.StartsWith("/api/webhooks/") || path.StartsWith("/hangfire");
    }
}
