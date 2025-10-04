using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DebtManager.Web.Services;

namespace DebtManager.Web.Areas.Client.Controllers;

[Area("Client")]
[Authorize(Policy = "RequireClientScope")]
public class CommunicationsController : Controller
{
    public IActionResult Index(string? search, int page = 1, int pageSize = 20)
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Communications";
        ViewBag.Search = search;
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        return View();
    }

    public IActionResult Details(int id)
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Message Details";
        ViewBag.MessageId = id;
        return View();
    }
}
