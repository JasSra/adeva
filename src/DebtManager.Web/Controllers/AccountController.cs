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

    public IActionResult SignOutUser()
    {
        return SignOut(new AuthenticationProperties { RedirectUri = "/" }, OpenIdConnectDefaults.AuthenticationScheme, CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
