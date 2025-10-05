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

var builder = WebApplication.CreateBuilder(args);

// Serilog basic setup
builder.Host.UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration));

builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();
builder.Services.AddHttpContextAccessor();

// Health checks
builder.Services.AddHealthChecks();

// EF Core
var cs = builder.Configuration.GetConnectionString("Default") ?? "Server=(localdb)\\MSSQLLocalDB;Database=DebtManager;Trusted_Connection=True;";
builder.Services.AddDbContext<AppDbContext>(opts => opts.UseSqlServer(cs));

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
var hangfireCs = builder.Configuration.GetConnectionString("Hangfire") ?? cs;
builder.Services.AddHangfire(cfg => cfg
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(hangfireCs));
builder.Services.AddHangfireServer();

// Branding resolver
builder.Services.AddScoped<BrandingResolverMiddleware>();
builder.Services.AddScoped<IAdminService, AdminService>();

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

// Initialize database (dev-friendly): drop/create, seed admin/config/articles
await DebtManager.Web.Data.DbInitializer.InitializeAsync(app.Services, app.Environment);

app.UseSerilogRequestLogging();

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

// Configure recurring jobs
DebtManager.Web.Jobs.NightlyJobs.ConfigureRecurringJobs();

app.Run();

// Expose Program for WebApplicationFactory in tests
public partial class Program { }
