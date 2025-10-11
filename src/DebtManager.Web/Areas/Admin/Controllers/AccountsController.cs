using Microsoft.AspNetCore.Mvc;
using DebtManager.Web.Services;
using DebtManager.Contracts.Audit;
using DebtManager.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DebtManager.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class AccountsController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _auditService;

    public AccountsController(UserManager<ApplicationUser> userManager, IAuditService auditService)
    {
        _userManager = userManager;
        _auditService = auditService;
    }

    public async Task<IActionResult> Index(string? search, string? role, Guid? orgId, int page = 1, int pageSize = 20)
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "User Accounts";
        ViewBag.Search = search;
        ViewBag.Role = role;
        ViewBag.OrgId = orgId;
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;

        var query = _userManager.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(u => u.Email!.Contains(search) || (u.UserName != null && u.UserName.Contains(search)));
        }

        if (orgId.HasValue)
        {
            // Join via profiles to filter by organization
            query = from u in _userManager.Users
                    join p in HttpContext.RequestServices.GetRequiredService<DebtManager.Infrastructure.Persistence.AppDbContext>().UserProfiles on u.Id equals p.UserId
                    where p.OrganizationId == orgId
                    select u;
        }

        var totalCount = await query.CountAsync();
        var users = await query
            .OrderByDescending(u => u.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.TotalCount = totalCount;
        ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        await _auditService.LogAsync("VIEW_ACCOUNTS", "Accounts", details: $"Searched: {search}, Role: {role}");

        return View(users);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Account Details";
        
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
        {
            return NotFound();
        }

        var roles = await _userManager.GetRolesAsync(user);
        ViewBag.Roles = roles;

        await _auditService.LogAsync("VIEW_ACCOUNT_DETAILS", "Account", id.ToString(), $"User: {user.Email}");

        return View(user);
    }

    public async Task<IActionResult> AssignRole(Guid id)
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Assign Role";
        
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
        {
            return NotFound();
        }

        ViewBag.User = user;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> UpdateRole(Guid id, string role)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
        {
            return NotFound();
        }

        await _auditService.LogAsync("UPDATE_USER_ROLE", "Account", id.ToString(), $"Role updated to: {role} for user: {user.Email}");

        TempData["Message"] = $"Role updated successfully for account {user.Email}";
        return RedirectToAction("Index");
    }
}
