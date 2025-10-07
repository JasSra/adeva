using DebtManager.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DebtManager.Web.Filters;

namespace DebtManager.Web.Areas.Client.Controllers;

[Area("Client")]
[Authorize(Policy = "RequireClientScope")]
[RequireOrganizationOnboarded]
public class HomeController : Controller
{
    public IActionResult Index()
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Client Dashboard";
        return View();
    }
}
