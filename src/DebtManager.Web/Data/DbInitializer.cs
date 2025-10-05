using System.Security.Claims;
using DebtManager.Contracts.Configuration;
using DebtManager.Infrastructure.Identity;
using DebtManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DebtManager.Web.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, IHostEnvironment env)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<AppDbContext>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = sp.GetRequiredService<RoleManager<ApplicationRole>>();
        var appConfig = sp.GetRequiredService<IAppConfigService>();

        // Dev/Staging: drop and recreate to avoid migrations
        if (!env.IsProduction())
        {
            await db.Database.EnsureDeletedAsync();
            await db.Database.EnsureCreatedAsync();
        }
        else
        {
            // Production fallback: ensure created without destructive ops
            await db.Database.EnsureCreatedAsync();
        }

        // Roles
        var roles = new[] { "Admin", "Client", "User" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new ApplicationRole(role));
            }
        }

        // Dev admin user seed (local identity)
        var adminEmail = "admin@local";
        var admin = await userManager.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);
        if (admin == null)
        {
            admin = new ApplicationUser
            {
                UserName = "admin",
                Email = adminEmail,
                EmailConfirmed = true,
                ExternalAuthId = "dev-seeded-admin"
            };
            await userManager.CreateAsync(admin, "Admin!23456");
        }
        if (!await userManager.IsInRoleAsync(admin, "Admin"))
        {
            await userManager.AddToRoleAsync(admin, "Admin");
        }

        // Seed config keys with sensible dev defaults
        await SeedConfigAsync(appConfig);

        // Seed articles/content
        await ArticleSeeder.SeedArticlesAsync(db);

        // Seed dummy data for dev/staging
        if (!env.IsProduction())
        {
            await DummyDataSeeder.SeedDummyDataAsync(db);
        }
    }

    private static async Task SeedConfigAsync(IAppConfigService cfg)
    {
        // Sentinel: Allow bypassing bootstrap if dev keys are present
        if (!await cfg.ExistsAsync("System:BootstrapComplete"))
        {
            await cfg.SetAsync("System:BootstrapComplete", "false");
        }

        // Stripe
        if (!await cfg.ExistsAsync("Stripe:SecretKey"))
            await cfg.SetAsync("Stripe:SecretKey", "sk_test_1234567890ABCDEFGHIJKLMNOPQRSTUVWXYZ", true);
        if (!await cfg.ExistsAsync("Stripe:WebhookSecret"))
            await cfg.SetAsync("Stripe:WebhookSecret", "whsec_test_1234567890abcdefghijklmnopqrstuvwxyz", true);

        // Twilio (optional)
        if (!await cfg.ExistsAsync("Twilio:AccountSid"))
            await cfg.SetAsync("Twilio:AccountSid", "AC00000000000000000000000000000000", true);
        if (!await cfg.ExistsAsync("Twilio:AuthToken"))
            await cfg.SetAsync("Twilio:AuthToken", "devauthtoken_devauthtoken_dev", true);
        if (!await cfg.ExistsAsync("Twilio:FromNumber"))
            await cfg.SetAsync("Twilio:FromNumber", "+10000000000");

        // ABR (stub unless base url provided)
        if (!await cfg.ExistsAsync("AbrApi:DefinitionUrl"))
            await cfg.SetAsync("AbrApi:DefinitionUrl", "https://abr.business.gov.au/ApiDocumentation");

        // OpenAI (optional)
        if (!await cfg.ExistsAsync("OpenAI:ApiKey"))
            await cfg.SetAsync("OpenAI:ApiKey", "sk-dev-please-change", true);
    }
}
