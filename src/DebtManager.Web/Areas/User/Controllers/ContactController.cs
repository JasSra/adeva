using DebtManager.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DebtManager.Web.Filters;

namespace DebtManager.Web.Areas.User.Controllers;

[Area("User")]
[Authorize(Policy = "RequireUserScope")]
[RequireDebtorOnboarded]
public class ContactController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        ViewBag.Title = "Contact Details";
        return View();
    }
}
