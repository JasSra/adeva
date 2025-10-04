using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DebtManager.Web.Services;

namespace DebtManager.Web.Areas.Client.Controllers;

[Area("Client")]
[Authorize(Policy = "RequireClientScope")]
public class DelegatedAccessController : Controller
{
    public IActionResult Index(string? search, int page = 1, int pageSize = 20)
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Delegated Access";
        ViewBag.Search = search;
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        return View();
    }

    public IActionResult Add()
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Add User Access";
        return View();
    }
}
