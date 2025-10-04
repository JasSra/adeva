using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DebtManager.Web.Controllers;

public class AccountController : Controller
{
    [AllowAnonymous]
    public IActionResult SignIn(string? returnUrl = null)
    {
        return Challenge(new AuthenticationProperties { RedirectUri = returnUrl ?? "/" }, OpenIdConnectDefaults.AuthenticationScheme);
    }

    [AllowAnonymous]
    public IActionResult SignInUser(string? returnUrl = null)
    {
        // Sign in with User/Debtor scope
        return Challenge(new AuthenticationProperties { RedirectUri = returnUrl ?? "/User" }, OpenIdConnectDefaults.AuthenticationScheme);
    }

    [AllowAnonymous]
    public IActionResult SignInClient(string? returnUrl = null)
    {
        // Sign in with Client/Business scope
        return Challenge(new AuthenticationProperties { RedirectUri = returnUrl ?? "/Client" }, OpenIdConnectDefaults.AuthenticationScheme);
    }

    [AllowAnonymous]
    public IActionResult SignInAdmin(string? returnUrl = null)
    {
        // Sign in with Admin scope
        return Challenge(new AuthenticationProperties { RedirectUri = returnUrl ?? "/Admin" }, OpenIdConnectDefaults.AuthenticationScheme);
    }

    public IActionResult SignOutUser()
    {
        return SignOut(new AuthenticationProperties { RedirectUri = "/" }, OpenIdConnectDefaults.AuthenticationScheme, CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
