using Microsoft.AspNetCore.Mvc;
using DebtManager.Web.Services;

namespace DebtManager.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class AccountsController : Controller
{
    public IActionResult Index(string? search, string? role, int page = 1, int pageSize = 20)
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "User Accounts";
        ViewBag.Search = search;
        ViewBag.Role = role;
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        return View();
    }

    public IActionResult Details(int id)
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Account Details";
        ViewBag.AccountId = id;
        return View();
    }

    public IActionResult AssignRole(int id)
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Assign Role";
        ViewBag.AccountId = id;
        return View();
    }

    [HttpPost]
    public IActionResult UpdateRole(int id, string role)
    {
        TempData["Message"] = $"Role updated successfully for account #{id}";
        return RedirectToAction("Index");
    }
}
