using DebtManager.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DebtManager.Web.Areas.User.Controllers;

[Area("User")]
[Authorize(Policy = "RequireUserScope")]
public class AuditController : Controller
{
    public IActionResult Index(int page = 1, int pageSize = 20)
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Audit Log";
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        return View();
    }
}
