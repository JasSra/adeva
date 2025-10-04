using Microsoft.AspNetCore.Mvc;
using DebtManager.Web.Services;

namespace DebtManager.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class DebtorsController : Controller
{
    public IActionResult Index(string? search, int page = 1, int pageSize = 20)
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Debtors Management";
        ViewBag.Search = search;
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        return View();
    }

    public IActionResult Details(int id)
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Debtor Details";
        ViewBag.DebtorId = id;
        return View();
    }

    public IActionResult Create()
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Create Debtor";
        return View();
    }

    public IActionResult Edit(int id)
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Edit Debtor";
        ViewBag.DebtorId = id;
        return View();
    }
}
