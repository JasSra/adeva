using Microsoft.AspNetCore.Http;

namespace DebtManager.Web.Middleware;

/// <summary>
/// In Development, optionally redirect unauthenticated requests to /Dev/FakeSignin
/// to streamline onboarding without external identity providers.
/// </summary>
public class DevAuthRedirectMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IHostEnvironment _env;
    private readonly IConfiguration _config;

    public DevAuthRedirectMiddleware(RequestDelegate next, IHostEnvironment env, IConfiguration config)
    {
        _next = next;
        _env = env;
        _config = config;
    }

    public async Task Invoke(HttpContext context)
    {
        if (_env.IsDevelopment())
        {
            var enabled = _config.GetValue<bool>("DevAuth:EnableFakeSignin");
            var autoRedirect = _config.GetValue<bool>("DevAuth:AutoRedirectUnauthenticated");

            if (enabled && autoRedirect)
            {
                var user = context.User;
                if (!(user?.Identity?.IsAuthenticated ?? false))
                {
                    var path = context.Request.Path.Value ?? string.Empty;
                    if (ShouldIntercept(path))
                    {
                        var returnUrl = context.Request.Path + context.Request.QueryString;
                        context.Response.Redirect($"/Dev/FakeSignin?returnUrl={Uri.EscapeDataString(returnUrl)}");
                        return;
                    }
                }
            }
        }

        await _next(context);
    }

    private static bool ShouldIntercept(string path)
    {
        path = path.ToLowerInvariant();
        // allow static and dev/auth endpoints and health
        string[] allowPrefixes = new[]
        {
            "/dev/", "/css/", "/js/", "/lib/", "/images/", "/img/", "/favicon", "/assets/",
            "/health/", "/signin-oidc", "/signout-callback-oidc", "/hangfire"
        };
        if (string.IsNullOrWhiteSpace(path) || path == "/") return true; // intercept home
        foreach (var p in allowPrefixes)
        {
            if (path.StartsWith(p)) return false;
        }
        return true;
    }
}
