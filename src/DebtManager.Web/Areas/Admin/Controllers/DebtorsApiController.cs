using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DebtManager.Infrastructure.Persistence;
using DebtManager.Domain.Debtors;

namespace DebtManager.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("Admin/api/debtors")] 
public class DebtorsApiController(AppDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? search, [FromQuery] string? sortBy, [FromQuery] string? sortDir, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        IQueryable<Debtor> q = db.Debtors.AsNoTracking().Include(d => d.Organization).Include(d => d.Debts);
        if (!string.IsNullOrWhiteSpace(search))
        {
            q = q.Where(d => d.ReferenceId.Contains(search) || (d.Email ?? "").Contains(search) || (d.Phone ?? "").Contains(search) || ((d.FirstName + " " + d.LastName).Contains(search)));
        }
        if (!string.IsNullOrWhiteSpace(sortBy))
        {
            var asc = (sortDir ?? "asc").Equals("asc", StringComparison.OrdinalIgnoreCase);
            q = sortBy switch
            {
                "Reference" => asc ? q.OrderBy(d => d.ReferenceId) : q.OrderByDescending(d => d.ReferenceId),
                "Name" => asc ? q.OrderBy(d => d.FirstName).ThenBy(d => d.LastName) : q.OrderByDescending(d => d.FirstName).ThenByDescending(d => d.LastName),
                "Email" => asc ? q.OrderBy(d => d.Email) : q.OrderByDescending(d => d.Email),
                _ => asc ? q.OrderBy(d => d.CreatedAtUtc) : q.OrderByDescending(d => d.CreatedAtUtc)
            };
        }
        var total = await q.CountAsync();
        var items = await q.Skip((page-1)*pageSize).Take(pageSize).ToListAsync();

        var sb = new StringBuilder();
        foreach (var d in items)
        {
            var name = ($"{d.FirstName} {d.LastName}").Trim();
            var totalDebt = d.Debts?.Sum(x => x.OutstandingPrincipal) ?? 0m;
            sb.Append("<tr class=\"hover:bg-gray-50 dark:hover:bg-gray-700\">");
            sb.Append("<td class=\"px-3 md:px-4 py-2 md:py-3\"><input type=\"checkbox\" value=\"").Append(d.Id).Append("\"></td>");
            sb.Append("<td class=\"px-3 md:px-4 py-2 md:py-3\">#").Append(System.Net.WebUtility.HtmlEncode(d.ReferenceId)).Append("</td>");
            sb.Append("<td class=\"px-3 md:px-4 py-2 md:py-3 font-medium\">").Append(System.Net.WebUtility.HtmlEncode(name)).Append("</td>");
            sb.Append("<td class=\"px-3 md:px-4 py-2 md:py-3\">").Append(System.Net.WebUtility.HtmlEncode(d.Email ?? "")).Append("</td>");
            sb.Append("<td class=\"px-3 md:px-4 py-2 md:py-3\">").Append(System.Net.WebUtility.HtmlEncode(d.Phone ?? "")).Append("</td>");
            sb.Append("<td class=\"px-3 md:px-4 py-2 md:py-3\">").Append(System.Net.WebUtility.HtmlEncode(d.Organization?.Name ?? "")).Append("</td>");
            sb.Append("<td class=\"px-3 md:px-4 py-2 md:py-3\">$").Append(totalDebt.ToString("N2")).Append("</td>");
            sb.Append("<td class=\"px-3 md:px-4 py-2 md:py-3 text-right whitespace-nowrap\"><a class=\"text-blue-600 hover:underline mr-3\" href=\"/Admin/Debtors/Details/").Append(d.Id).Append("\">View</a><a class=\"text-blue-600 hover:underline\" href=\"/Admin/Debtors/Edit/").Append(d.Id).Append("\">Edit</a></td>");
            sb.Append("</tr>");
        }

        return Json(new { total, page, pageSize, rowsHtml = sb.ToString() });
    }
}
