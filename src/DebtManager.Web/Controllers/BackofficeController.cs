using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DebtManager.Web.Services;

namespace DebtManager.Web.Controllers;

/// <summary>
/// Backoffice controller for system administration tasks
/// Shows one-time admin signup link when no admin exists
/// </summary>
[AllowAnonymous]
public class BackofficeController : Controller
{
    private readonly IAdminService _adminService;

    public BackofficeController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var isInitialSignupAllowed = await _adminService.IsInitialAdminSignupAllowedAsync(ct);
        ViewBag.IsInitialAdminSignupAllowed = isInitialSignupAllowed;
        
        if (!isInitialSignupAllowed)
        {
            ViewBag.Message = "Admin users already exist. Admin signup is restricted. Only existing administrators can assign admin roles to other users.";
        }
        
        return View();
    }
}
