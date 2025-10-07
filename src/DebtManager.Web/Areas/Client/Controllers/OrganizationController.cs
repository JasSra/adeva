using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DebtManager.Web.Filters;

namespace DebtManager.Web.Areas.Client.Controllers;

[Area("Client")]
[Authorize(Policy = "RequireClientScope")]
[RequireOrganizationOnboarded]
public class OrganizationController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        ViewBag.Title = "Organization Profile";
        return View();
    }
}
