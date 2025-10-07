using DebtManager.Domain.Communications;
using DebtManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DebtManager.Web.ViewComponents;

public class UnreadBadgeUserViewComponent : ViewComponent
{
    private readonly AppDbContext _db;
    public UnreadBadgeUserViewComponent(AppDbContext db) { _db = db; }

    public async Task<IViewComponentResult> InvokeAsync(CancellationToken ct = default)
    {
        var userIdStr = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId)) return View("Default", 0);
        var count = await _db.InternalMessageRecipients
            .CountAsync(r => r.UserId == userId && r.Status == InternalMessageStatus.Unread, ct);
        return View("Default", count);
    }
}
