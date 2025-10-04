using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DebtManager.Web.Services;

namespace DebtManager.Web.Areas.Client.Controllers;

[Area("Client")]
[Authorize(Policy = "RequireClientScope")]
public class AuditController : Controller
{
    public IActionResult Index(string? search, string? eventType, int page = 1, int pageSize = 20)
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Audit Log";
        ViewBag.Search = search;
        ViewBag.EventType = eventType;
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        return View();
    }
}
