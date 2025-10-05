using DebtManager.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace DebtManager.Web.Auth;

public static class IdentitySeed
{
    public static async Task EnsureRolesAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        var roles = new[] { "SuperAdmin", "Admin", "Client", "User" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new ApplicationRole(role));
            }
        }
    }
}
