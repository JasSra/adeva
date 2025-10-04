using Hangfire.Dashboard;

namespace DebtManager.Web;

public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        
        // Check if user is authenticated
        if (!httpContext.User.Identity?.IsAuthenticated ?? true)
        {
            return false;
        }

        // Check if user has Admin scope
        var adminScope = httpContext.RequestServices
            .GetRequiredService<IConfiguration>()
            .GetValue<string>("AzureB2CScopes:Admin");

        if (string.IsNullOrWhiteSpace(adminScope))
        {
            return false;
        }

        return httpContext.User.HasClaim("scp", adminScope);
    }
}
