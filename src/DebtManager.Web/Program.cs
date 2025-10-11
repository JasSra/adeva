using DebtManager.Web;
using DebtManager.Web.Services;
using Microsoft.AspNetCore.Mvc;
using DebtManager.Infrastructure;
using DebtManager.Infrastructure.Persistence;
using DebtManager.Application;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Microsoft.Identity.Web;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using DebtManager.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using DebtManager.Web.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using DebtManager.Web.Middleware;
using DebtManager.Web.Jobs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web.Resource;
using Microsoft.IdentityModel.Logging;
using System.Data;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Caching.Distributed;
using DebtManager.Contracts.Configuration;
 

var builder = WebApplication.CreateBuilder(args);

// Enable IdentityModel PII logging if configured (use with caution in production)
IdentityModelEventSource.ShowPII = builder.Configuration.GetValue<bool>("AzureAd:EnablePiiLogging");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

// Serilog basic setup (keep config-driven to avoid eager provider initialization)
// Tip: Use appsettings to reduce noisy providers at boot (e.g., Microsoft to Warning)
builder.Host.UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration));

// MVC + conditionally add runtime compilation only for local dev
var mvcBuilder = builder.Services.AddControllersWithViews();
if (builder.Environment.IsDevelopment())
{
    mvcBuilder.AddRazorRuntimeCompilation();
}

builder.Services.AddHttpContextAccessor();

// Maintenance state
builder.Services.AddSingleton<DebtManager.Web.Services.IMaintenanceState, DebtManager.Web.Services.MaintenanceState>();

// Health checks
builder.Services.AddHealthChecks();

// Distributed cache (Redis)
var redisCs = builder.Configuration.GetConnectionString("Redis")
    ?? builder.Configuration["Redis:ConnectionString"]
    ?? Environment.GetEnvironmentVariable("REDIS_CONNECTION");
if (!string.IsNullOrWhiteSpace(redisCs))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisCs;
        options.InstanceName = "adeva:";
    });
}
else
{
    // Fallback: in-memory dist cache (optional). If not added, AppConfigService will fallback to local cache.
    // builder.Services.AddDistributedMemoryCache();
}

// EF Core
// Prefer Default; fall back to Azure Connected Services name "DatabaseConnection" if present
var defaultCs = builder.Configuration.GetConnectionString("Default");
var svcCs = builder.Configuration.GetConnectionString("DatabaseConnection");
var cs = defaultCs ?? svcCs ?? "Server=(localdb)\\MSSQLLocalDB;Database=DebtManager;Trusted_Connection=True;";

var efSensitive = builder.Configuration.GetValue("Diagnostics:EfCore:SensitiveDataLogging", false);
var efDetailed = builder.Configuration.GetValue("Diagnostics:EfCore:DetailedErrors", false);

builder.Services.AddDbContext<AppDbContext>(opts =>
{
    opts.UseSqlServer(cs);
    if (efSensitive) opts.EnableSensitiveDataLogging();
    if (efDetailed) opts.EnableDetailedErrors();
});

// Log effective connection string source (sanitized)
try
{
    var which = defaultCs != null ? "ConnectionStrings:Default" : (svcCs != null ? "ConnectionStrings:DatabaseConnection" : "fallback-localdb");
    var safeCs = cs?.Replace("Password=", "Password=***").Replace("Pwd=", "Pwd=***");
    Log.Information("DB connection source: {Source}. Hangfire source may differ. Effective CS (sanitized): {CS}", which, safeCs);
}
catch { }

// Identity Core using EF stores
builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = false; // External identity provides uniqueness by oid
        options.SignIn.RequireConfirmedAccount = false;
        options.Tokens.AuthenticatorTokenProvider = TokenOptions.DefaultAuthenticatorProvider;
    })
    .AddRoles<ApplicationRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

// Configure authentication: Cookies as default auth, OIDC as challenge.
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddMicrosoftIdentityWebApp(options =>
    {
        var b2c = builder.Configuration.GetSection("AzureAdB2C");
        options.ClientId = b2c["ClientId"]!;
        options.Instance = b2c["Instance"] ?? "https://jsraauth.b2clogin.com/";
        options.Domain = b2c["Domain"] ?? "jsraauth.onmicrosoft.com";
        options.Authority = b2c["Authority"]!;
        options.ClientSecret = b2c["ClientSecret"]; // Added client secret
        options.CallbackPath = b2c["CallbackPath"] ?? "/signin-oidc";
        options.SignedOutCallbackPath = b2c["SignedOutCallbackPath"] ?? "/signout-callback-oidc";
        options.TokenValidationParameters.NameClaimType = "name";
        options.Events = new OpenIdConnectEvents
        {
            OnTokenValidated = async ctx => await TokenValidatedHandler.OnTokenValidated(ctx)
        };
    })
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches();

// Configure the cookie handler options (scheme added by MicrosoftIdentityWeb)
builder.Services.Configure<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.LoginPath = "/Dev/Login";
    options.AccessDeniedPath = "/Dev/Login";
    options.SlidingExpiration = true;
});

// Claims transformation to map scopes -> roles
builder.Services.AddScoped<IClaimsTransformation, B2CRoleClaimsTransformation>();

// Hangfire (SQL Server for persistence and audit)
var hangfireCs = builder.Configuration.GetConnectionString("Hangfire")
                 ?? defaultCs
                 ?? svcCs
                 ?? cs;

try
{
    var hangfireSource = builder.Configuration.GetConnectionString("Hangfire") != null ? "ConnectionStrings:Hangfire" : (defaultCs != null ? "ConnectionStrings:Default" : (svcCs != null ? "ConnectionStrings:DatabaseConnection" : "fallback-localdb"));
    var safeHf = hangfireCs?.Replace("Password=", "Password=***").Replace("Pwd=", "Pwd=***");
    Log.Information("Hangfire connection source: {Source}. Effective CS (sanitized): {CS}", hangfireSource, safeHf);
}
catch { }

builder.Services.AddHangfire(cfg => cfg
    .UseSimpleAssemblyNameTypeSerializer()
    // Avoid forcing Newtonsoft/advanced serializer settings to keep startup lean
    .UseSqlServerStorage(hangfireCs));
// Remove AddHangfireServer to avoid crashing the host when storage is unavailable
// builder.Services.AddHangfireServer();

// Branding resolver
builder.Services.AddScoped<BrandingResolverMiddleware>();
builder.Services.AddScoped<IAdminService, AdminService>();

// Messaging services (generic + onboarding orchestration)
builder.Services.AddScoped<IMessageQueueService, MessageQueueService>();
builder.Services.AddScoped<IOnboardingNotificationService, OnboardingNotificationService>();
builder.Services.AddScoped<MessageDispatchJob>();

// ABR business lookup using runtime-managed API key (via IAppConfigService)
builder.Services.AddScoped<IBusinessLookupService>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<AbrBusinessLookupService>>();
    var cfg = sp.GetRequiredService<IAppConfigService>();
    var key = cfg.GetAsync("AbrApi:ApiKey").GetAwaiter().GetResult() ?? string.Empty;
    return new AbrBusinessLookupService(key, logger);
});

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

builder.Services.AddAuthorization(options =>
{
    var scopes = builder.Configuration.GetSection("AzureB2CScopes");
    var userScope = scopes["User"] ?? string.Empty;
    var clientScope = scopes["Client"] ?? string.Empty;
    var adminScope = scopes["Admin"] ?? string.Empty;
    options.AddPolicy("RequireUserScope", policy =>
        policy.RequireAssertion(ctx => ctx.User.HasClaim("scp", userScope)));
    options.AddPolicy("RequireClientScope", policy =>
        policy.RequireAssertion(ctx => ctx.User.HasClaim("scp", clientScope)));
    options.AddPolicy("RequireAdminScope", policy =>
        policy.RequireAssertion(ctx => ctx.User.HasClaim("scp", adminScope)));
});

var app = builder.Build();

// Optionally turn on the Developer Exception Page in non-dev when explicitly enabled
if (!app.Environment.IsDevelopment() && app.Configuration.GetValue("Diagnostics:UseDeveloperExceptionPage", false))
{
    app.UseDeveloperExceptionPage();
}

// Initialize database (lean): migrate; optional seed is handled inside initializer via config flags
var maintenance = app.Services.GetRequiredService<IMaintenanceState>();
try
{
    // await DebtManager.Web.Data.DbInitializer.InitializeAsync(app.Services, app.Environment);
}
catch (Exception ex)
{
    // Enter maintenance mode and log the startup exception. App will stay up and serve 503.
    maintenance.Enable(ex);
    Log.ForContext("Startup", true).Error(ex, "Startup initialization failed; entering maintenance mode.");
}

app.UseSerilogRequestLogging();

// Maintenance mode should be evaluated as early as possible
app.UseMaintenanceMode();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

// Dev-only auth redirect middleware
if (app.Environment.IsDevelopment() && app.Configuration.GetValue<bool>("DevAuth:EnableFakeSignin"))
{
    app.UseMiddleware<DevAuthRedirectMiddleware>();
}

app.UseAuthorization();

// Enforce security onboarding (TOTP, phone, client org)
app.UseMiddleware<SecurityEnforcementMiddleware>();

// Admin bootstrap nudge (after auth)
app.UseMiddleware<BootstrapNudgeMiddleware>();

app.UseMiddleware<BrandingResolverMiddleware>();

app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");

// Diagnostics: SQL probe endpoint (guarded by config flag)
if (app.Configuration.GetValue("Diagnostics:EnableSqlProbe", false))
{
    app.MapGet("/_diag/sql", async (AppDbContext db) =>
    {
        try
        {
            var conn = db.Database.GetDbConnection();
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT DB_NAME() AS DbName, SUSER_SNAME() AS LoginName, USER_NAME() AS UserName";
            await using var reader = await cmd.ExecuteReaderAsync();
            string dbName = ""; string loginName = ""; string userName = "";
            if (await reader.ReadAsync())
            {
                dbName = reader["DbName"].ToString() ?? "";
                loginName = reader["LoginName"].ToString() ?? "";
                userName = reader["UserName"].ToString() ?? "";
            }
            return Results.Json(new { ok = true, dbName, loginName, userName });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "SQL probe failed");
            return Results.Json(new { ok = false, error = ex.Message });
        }
    });
}

// Hangfire Dashboard - Secured with Admin scope
app.MapHangfireDashboard("/hangfire", new Hangfire.DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});

// Areas routing
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

// Default route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

var scopeRequiredByApi = app.Configuration["AzureAd:Scopes"];

// Simple API for recipient search (admin)
app.MapGet("/api/admin/usersearch", async ([FromQuery] string q, AppDbContext db,HttpContext httpContext) =>
{
    httpContext.VerifyUserHasAnyAcceptedScope(scopeRequiredByApi);
    if (string.IsNullOrWhiteSpace(q) || q.Length < 2) return Results.Json(Array.Empty<object>());
    q = q.ToLowerInvariant();
    var results = await db.Users
        .Where(u => (u.Email ?? "").ToLower().Contains(q) || (u.UserName ?? "").ToLower().Contains(q))
        .OrderBy(u => u.Email)
        .Take(10)
        .Select(u => new { id = u.Id, name = u.UserName ?? u.Email ?? "Unknown", email = u.Email ?? string.Empty })
        .ToListAsync();
    return Results.Json(results);
}).RequireAuthorization(new AuthorizeAttribute { Policy = "RequireAdminScope" });

// Configure recurring jobs only if not in maintenance and server is enabled
if (!maintenance.IsMaintenance)
{
    var serverEnabled = app.Configuration.GetValue<bool>("Hangfire:ServerEnabled");
    if (serverEnabled)
    {
        BackgroundJobServer? server = null;
        try
        {
            server = new BackgroundJobServer();
            // Configure recurring jobs
            DebtManager.Web.Jobs.NightlyJobs.ConfigureRecurringJobs();
            RecurringJob.AddOrUpdate("dispatch-queued-messages-di", () => app.Services.GetRequiredService<MessageDispatchJob>().RunAsync(CancellationToken.None), Cron.Minutely);
            // Dispose server gracefully on shutdown
            var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
            lifetime.ApplicationStopping.Register(() => server.Dispose());
        }
        catch (Exception ex)
        {
            maintenance.Enable(ex);
            Log.Error(ex, "Failed to start Hangfire server; entering maintenance mode.");
        }
    }
}

app.Run();

// Expose Program for WebApplicationFactory in tests
public partial class Program { }

// For convenience: you can bundle lean startup tuning into a helper and call it here.
// Example (not invoked):
// LeanStartup.Apply(builder); // Uncomment to apply additional lean settings in one place.
