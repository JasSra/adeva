using DebtManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DebtManager.Web.ViewComponents;

public class ApplicationsBadgeViewComponent : ViewComponent
{
    private readonly AppDbContext _db;
    public ApplicationsBadgeViewComponent(AppDbContext db) { _db = db; }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var count = await _db.Organizations.CountAsync(o => !o.IsApproved);
        return View("Default", count);
    }
}
