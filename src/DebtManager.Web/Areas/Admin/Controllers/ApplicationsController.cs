using Microsoft.AspNetCore.Mvc;
using DebtManager.Web.Services;

namespace DebtManager.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class ApplicationsController : Controller
{
    public IActionResult Index(string? search, int page = 1, int pageSize = 20)
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Pending Applications";
        ViewBag.Search = search;
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        return View();
    }

    public IActionResult Details(int id)
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Application Details";
        ViewBag.ApplicationId = id;
        return View();
    }

    [HttpPost]
    public IActionResult Approve(int id)
    {
        TempData["Message"] = $"Application #{id} approved successfully";
        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult Reject(int id, string reason)
    {
        TempData["Message"] = $"Application #{id} rejected";
        return RedirectToAction("Index");
    }
}
