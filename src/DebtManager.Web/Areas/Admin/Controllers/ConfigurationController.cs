using Microsoft.AspNetCore.Mvc;
using DebtManager.Web.Services;
using Microsoft.AspNetCore.Authorization;

namespace DebtManager.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "RequireAdminScope")]
public partial class ConfigurationController : Controller
{
    public IActionResult Index()
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "System Configuration";
        return View();
    }

    public IActionResult Fees()
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Fee Configuration";
        return View();
    }

    public IActionResult Branding()
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Branding Configuration";
        return View();
    }

    public IActionResult Integrations()
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Integration Settings";
        return View();
    }
}
