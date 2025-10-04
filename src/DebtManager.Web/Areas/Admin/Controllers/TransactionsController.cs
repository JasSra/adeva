using Microsoft.AspNetCore.Mvc;
using DebtManager.Web.Services;

namespace DebtManager.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class TransactionsController : Controller
{
    public IActionResult Index(string? search, string? type, DateTime? fromDate, DateTime? toDate, int page = 1, int pageSize = 20)
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Transactions";
        ViewBag.Search = search;
        ViewBag.Type = type;
        ViewBag.FromDate = fromDate;
        ViewBag.ToDate = toDate;
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        return View();
    }

    public IActionResult Details(int id)
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Transaction Details";
        ViewBag.TransactionId = id;
        return View();
    }
}
