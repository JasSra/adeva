using DebtManager.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace DebtManager.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class HomeController : Controller
{
    public IActionResult Index()
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        return View();
    }
}
