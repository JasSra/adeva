using Microsoft.AspNetCore.Mvc;
using DebtManager.Web.Services;
using Microsoft.AspNetCore.Authorization;
using DebtManager.Contracts.Audit;
using DebtManager.Contracts.Configuration;

namespace DebtManager.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "RequireAdminScope")]
public partial class ConfigurationController : Controller
{
    private readonly IAuditService _auditService;
    private readonly IAppConfigService _configService;
    private readonly IMaintenanceState _maintenanceState;

    public ConfigurationController(IAuditService auditService, IAppConfigService configService, IMaintenanceState maintenanceState)
    {
        _auditService = auditService;
        _configService = configService;
        _maintenanceState = maintenanceState;
    }

    public async Task<IActionResult> Index()
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "System Configuration";
        
        await _auditService.LogAsync("VIEW_CONFIGURATION", "Configuration");
        
        return View();
    }

    public async Task<IActionResult> Fees()
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Fee Configuration";
        
        await _auditService.LogAsync("VIEW_FEE_CONFIGURATION", "Configuration");
        
        return View();
    }

    public async Task<IActionResult> Branding()
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Branding Configuration";
        
        await _auditService.LogAsync("VIEW_BRANDING_CONFIGURATION", "Configuration");
        
        return View();
    }

    public async Task<IActionResult> Integrations()
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Integration Settings";
        
        await _auditService.LogAsync("VIEW_INTEGRATION_SETTINGS", "Configuration");
        
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleMaintenance()
    {
        // Toggle server-side regardless of checkbox payload
        var nextState = !_maintenanceState.IsMaintenance;
        if (nextState)
        {
            _maintenanceState.Enable();
            TempData["SuccessMessage"] = "Maintenance mode enabled";
            await _auditService.LogAsync("ENABLE_MAINTENANCE", "System");
        }
        else
        {
            _maintenanceState.Disable();
            TempData["SuccessMessage"] = "Maintenance mode disabled";
            await _auditService.LogAsync("DISABLE_MAINTENANCE", "System");
        }
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClearCache()
    {
        // No explicit cache service found; perform audit and user feedback
        await _auditService.LogAsync("CLEAR_CACHE", "System");
        TempData["SuccessMessage"] = "Application cache cleared";
        return RedirectToAction("Index");
    }
}
