# Maintenance Mode & Database Migrations - Configuration Summary

## ? Current Status

### Database Migrations
**Already Enabled!** ?

Migrations run automatically on application startup in `DbInitializer.cs`:

```csharp
// Line 20 in DbInitializer.cs
await db.Database.MigrateAsync();
```

**How it works:**
1. Application starts
2. `Program.cs` calls `DbInitializer.InitializeAsync()` (line 111)
3. Migrations execute before any other initialization
4. If migrations fail, app enters maintenance mode automatically

**Error Handling:**
```csharp
try
{
    await DebtManager.Web.Data.DbInitializer.InitializeAsync(app.Services, app.Environment);
}
catch (Exception ex)
{
    maintenance.Enable(ex);
    Log.ForContext("Startup", true).Error(ex, "Startup initialization failed; entering maintenance mode.");
}
```

### Maintenance Mode
**Already Implemented!** ?

The maintenance page has a modern, professional design featuring:

#### Visual Design
- **Dark gradient background** with blur effects
- **Glassmorphic card** design with border glow
- **Animated pulsing badge** showing 503 status
- **Adeva branding** with logo and wordmark
- **Clean typography** using Inter font

#### Information Displayed
- Clear "We'll be right back" message
- Reference ID (trace ID) for support
- Start time of maintenance
- Duration counter
- Development error details (in dev mode only)

#### Technical Features
- **503 Service Unavailable** status code
- **Retry-After** header (120 seconds)
- **X-Trace-Id** header for debugging
- Health endpoints still accessible (`/health/*`)
- Brand assets still accessible (`/brand/*`)

## Configuration Options

### Maintenance Mode Triggers

#### 1. Database Migration Failure
Automatic - App enters maintenance if migrations fail on startup.

#### 2. Hangfire Server Failure
Automatic - App enters maintenance if Hangfire server fails to start.

```csharp
// Program.cs lines 137-153
if (!maintenance.IsMaintenance)
{
    var serverEnabled = app.Configuration.GetValue<bool>("Hangfire:ServerEnabled");
    if (serverEnabled)
    {
        BackgroundJobServer? server = null;
        try
        {
            server = new BackgroundJobServer();
            // ...
        }
        catch (Exception ex)
        {
            maintenance.Enable(ex);
            Log.Error(ex, "Failed to start Hangfire server; entering maintenance mode.");
        }
    }
}
```

#### 3. Manual Trigger
You can programmatically enable maintenance mode:

```csharp
// Inject IMaintenanceState
var maintenance = app.Services.GetRequiredService<IMaintenanceState>();

// Enable maintenance
maintenance.Enable();

// Or with exception details
maintenance.Enable(new Exception("Planned maintenance"));

// Disable maintenance
maintenance.Disable();
```

### Configuration Settings

**appsettings.json:**
```json
{
  "Hangfire": {
    "ServerEnabled": true  // Set to false to disable Hangfire server
  },
  "DevAuth": {
    "EnableFakeSignin": true  // Dev mode only
  },
  "Content": {
    "SeedArticlesOnStartup": false
  },
  "DevData": {
    "SeedOnStartup": false
  }
}
```

## Startup Sequence

1. **App Builder Configuration**
   - Serilog logging
   - MVC with runtime compilation (dev only)
   - DbContext with connection string
   - Identity configuration
   - Authentication (Azure AD B2C + Cookies)
   - Hangfire configuration

2. **Database Initialization** ?
   ```csharp
   await db.Database.MigrateAsync();  // MIGRATIONS RUN HERE
   ```
   - Roles seeded
   - Dev admin user created
   - Config keys seeded
   - Message templates seeded
   - Optional: Articles and dummy data

3. **Middleware Pipeline**
   - Serilog request logging
   - **Maintenance mode check** (early in pipeline)
   - Exception handler
   - HTTPS redirection
   - Static files
   - Routing
   - Authentication
   - Dev auth redirect (dev only)
   - Authorization
   - Security enforcement
   - Bootstrap nudge
   - Branding resolver

4. **Hangfire Server Start** (if enabled)
   - Background job server starts
   - Recurring jobs configured

## Maintenance Page Customization

### Current Features

**Always Displayed:**
- Adeva logo and branding
- Main heading
- Subtitle message
- Reference ID (for support)
- Footer with support instructions

**Conditional Display:**
- Start time (if maintenance was triggered)
- Duration counter
- Error stack trace (development mode only)

### Customization Points

You can customize the maintenance page in `MaintenanceModeMiddleware.cs`:

```csharp
// Line 84: Main heading
sb.Append("<h1>We'll be right back</h1>");

// Line 85: Subtitle
sb.Append("<p class=\"subtitle\">We're experiencing technical difficulties...</p>");

// Line 102: Footer message
sb.Append("Please reference the ID above when contacting support...");
```

### Styling
The maintenance page uses inline CSS with:
- Modern gradients
- Glassmorphic effects
- Responsive design
- Dark theme
- Professional color scheme
- Mobile-friendly layout

## Health Checks

Health endpoints remain accessible during maintenance:

- `GET /health/live` - Liveness probe
- `GET /health/ready` - Readiness probe

This ensures orchestrators (Kubernetes, Azure App Service) can monitor the application even during maintenance.

## Testing Maintenance Mode

### 1. Test Database Migration Failure

Temporarily break the connection string:

```json
// appsettings.Development.json
{
  "ConnectionStrings": {
    "Default": "Server=invalid;Database=Test;Trusted_Connection=True;"
  }
}
```

Start the app ? Should show maintenance page with error details.

### 2. Test Manual Maintenance

Create a controller action to toggle maintenance:

```csharp
[Authorize(Roles = "SuperAdmin")]
[HttpPost]
public IActionResult EnableMaintenance([FromServices] IMaintenanceState maintenance)
{
    maintenance.Enable();
    return Ok("Maintenance mode enabled");
}

[Authorize(Roles = "SuperAdmin")]
[HttpPost]
public IActionResult DisableMaintenance([FromServices] IMaintenanceState maintenance)
{
    maintenance.Disable();
    return Ok("Maintenance mode disabled");
}
```

### 3. Test Hangfire Failure

Set invalid Hangfire connection string and enable server:

```json
{
  "ConnectionStrings": {
    "Hangfire": "Server=invalid;Database=Hangfire;Trusted_Connection=True;"
  },
  "Hangfire": {
    "ServerEnabled": true
  }
}
```

## Production Recommendations

### 1. Connection String Configuration

Use Azure Key Vault or environment variables:

```csharp
// Option 1: Environment variables
"ConnectionStrings:Default": "Server=..."

// Option 2: Azure App Configuration
builder.Configuration.AddAzureAppConfiguration(options => { ... });
```

### 2. Graceful Shutdown

The app already handles graceful shutdown for Hangfire:

```csharp
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStopping.Register(() => server.Dispose());
```

### 3. Monitoring

Set up alerts for:
- Maintenance mode activation
- Migration failures
- 503 status code spike

### 4. Planned Maintenance

For planned maintenance, you can:

1. **Pre-announce** via banner notification
2. **Enable maintenance mode** programmatically
3. **Perform updates** (migrations, config changes)
4. **Disable maintenance mode**
5. **Verify health checks** pass

## Troubleshooting

### Issue: Migrations not running

**Check:**
1. Connection string is valid
2. SQL Server is accessible
3. User has CREATE TABLE permissions
4. No firewall blocking connection

**Logs to check:**
```
[Error] Startup initialization failed; entering maintenance mode
```

### Issue: Maintenance page not showing

**Check:**
1. `UseMaintenanceMode()` is in middleware pipeline (it is - line 118)
2. Maintenance mode is actually enabled
3. Request path is not `/health/*` or `/brand/*`

### Issue: Can't exit maintenance mode

Maintenance mode persists for the application lifetime. To exit:
1. Call `maintenance.Disable()`
2. Or restart the application

## Summary

? **Database migrations**: Already enabled, run on every startup
? **Maintenance page**: Professional design, already implemented
? **Error handling**: Automatic maintenance mode on startup failures
? **Health checks**: Remain accessible during maintenance
? **Logging**: Full Serilog integration
? **Development mode**: Shows error details
? **Production ready**: Clean, professional maintenance page

**No changes needed!** Everything is already properly configured.

---

**Related Files:**
- `Program.cs` - Application startup and configuration
- `DbInitializer.cs` - Database initialization and migrations
- `MaintenanceModeMiddleware.cs` - Maintenance mode middleware
- `IMaintenanceState.cs` / `MaintenanceState.cs` - Maintenance state management
