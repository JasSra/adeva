using DebtManager.Web.Services;
using DebtManager.Contracts.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace DebtManager.Web.Controllers;

public class PaymentController : Controller
{
    private readonly IDebtRepository _debtRepository;

    public PaymentController(IDebtRepository debtRepository)
    {
        _debtRepository = debtRepository;
    }

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

    public async Task<IActionResult> SelectPlan(Guid debtId)
    {
        var debt = await _debtRepository.GetAsync(debtId);
        if (debt == null)
        {
            return NotFound();
        }

        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Choose Your Payment Plan";
        ViewBag.DebtId = debtId;
        ViewBag.DebtAmount = debt.OutstandingPrincipal;
        ViewBag.Currency = debt.Currency;
        return View();
    }

    public IActionResult PlanConfirmation(Guid paymentPlanId)
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Payment Plan Confirmed";
        ViewBag.PaymentPlanId = paymentPlanId;
        return View();
    }
}
