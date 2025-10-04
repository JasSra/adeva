using DebtManager.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace DebtManager.Web.Controllers;

public class PaymentController : Controller
{
    public IActionResult Anonymous(string? reference)
    {
        // For anonymous payment, use organization's branding based on debt reference
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Make a Payment";
        ViewBag.Reference = reference;
        return View();
    }

    public IActionResult VerifyOtp(string reference, string contact)
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Verify Identity";
        ViewBag.Reference = reference;
        ViewBag.Contact = contact;
        return View();
    }
}
