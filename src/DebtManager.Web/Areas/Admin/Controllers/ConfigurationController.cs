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

    public ConfigurationController(IAuditService auditService, IAppConfigService configService)
    {
        _auditService = auditService;
        _configService = configService;
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
}
