using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DebtManager.Infrastructure.Persistence;
using DebtManager.Domain.Debts;
using System.Linq;

namespace DebtManager.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("Admin/api/debts")] 
public class DebtsApiController(AppDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? search, [FromQuery] string? status, [FromQuery] string? sortBy, [FromQuery] string? sortDir, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        IQueryable<Debt> q = db.Debts.AsNoTracking().Include(d => d.Debtor).Include(d => d.Organization);
        if (!string.IsNullOrWhiteSpace(search))
        {
            q = q.Where(d => d.ClientReferenceNumber.Contains(search) || (d.Debtor != null && ((d.Debtor.FirstName + " " + d.Debtor.LastName).Contains(search))));
        }
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<DebtStatus>(status, true, out var st))
        {
            q = q.Where(d => d.Status == st);
        }
        if (!string.IsNullOrWhiteSpace(sortBy))
        {
            var asc = (sortDir ?? "asc").Equals("asc", StringComparison.OrdinalIgnoreCase);
            q = sortBy switch
            {
                "Id" => asc ? q.OrderBy(d => d.Id) : q.OrderByDescending(d => d.Id),
                "Debtor" => asc ? q.OrderBy(d => d.Debtor!.LastName).ThenBy(d => d.Debtor!.FirstName) : q.OrderByDescending(d => d.Debtor!.LastName).ThenByDescending(d => d.Debtor!.FirstName),
                "Org" => asc ? q.OrderBy(d => d.Organization!.Name) : q.OrderByDescending(d => d.Organization!.Name),
                "Amount" => asc ? q.OrderBy(d => d.OriginalPrincipal) : q.OrderByDescending(d => d.OriginalPrincipal),
                "Outstanding" => asc ? q.OrderBy(d => d.OutstandingPrincipal) : q.OrderByDescending(d => d.OutstandingPrincipal),
                _ => asc ? q.OrderBy(d => d.CreatedAtUtc) : q.OrderByDescending(d => d.CreatedAtUtc)
            };
        }
        var total = await q.CountAsync();
        var items = await q.Skip((page-1)*pageSize).Take(pageSize).ToListAsync();

        var sb = new StringBuilder();
        foreach (var d in items)
        {
            sb.Append("<tr class=\"hover:bg-gray-50 dark:hover:bg-gray-700\">");
            sb.Append("<td class=\"px-3 md:px-4 py-2 md:py-3\"><input type=\"checkbox\" value=\"").Append(d.Id).Append("\"></td>");
            sb.Append("<td class=\"px-3 md:px-4 py-2 md:py-3\">#D-").Append(d.Id.ToString()[..6]).Append("</td>");
            var debtorName = d.Debtor != null ? System.Net.WebUtility.HtmlEncode($"{d.Debtor.FirstName} {d.Debtor.LastName}") : string.Empty;
            var orgName = d.Organization?.Name != null ? System.Net.WebUtility.HtmlEncode(d.Organization.Name) : string.Empty;
            sb.Append("<td class=\"px-3 md:px-4 py-2 md:py-3\">").Append(debtorName).Append("</td>");
            sb.Append("<td class=\"px-3 md:px-4 py-2 md:py-3\">").Append(orgName).Append("</td>");
            sb.Append("<td class=\"px-3 md:px-4 py-2 md:py-3\">$").Append(d.OriginalPrincipal.ToString("N2")).Append("</td>");
            sb.Append("<td class=\"px-3 md:px-4 py-2 md:py-3 font-medium\">$").Append(d.OutstandingPrincipal.ToString("N2")).Append("</td>");
            var statusPill = d.Status switch { DebtStatus.Active => "<span class=\\\"px-2 py-1 text-xs rounded-full bg-green-100 text-green-800\\\">Active</span>", DebtStatus.Settled => "<span class=\\\"px-2 py-1 text-xs rounded-full bg-gray-100 text-gray-800\\\">Settled</span>", DebtStatus.InArrears => "<span class=\\\"px-2 py-1 text-xs rounded-full bg-yellow-100 text-yellow-800\\\">In Arrears</span>", DebtStatus.Disputed => "<span class=\\\"px-2 py-1 text-xs rounded-full bg-red-100 text-red-800\\\">Disputed</span>", _ => "<span class=\\\"px-2 py-1 text-xs rounded-full bg-blue-100 text-blue-800\\\">Open</span>" };
            sb.Append("<td class=\"px-3 md:px-4 py-2 md:py-3\">").Append(statusPill).Append("</td>");
            sb.Append("<td class=\"px-3 md:px-4 py-2 md:py-3 text-right whitespace-nowrap\"><a class=\"text-blue-600 hover:underline mr-3\" href=\"/Admin/Debts/Details/").Append(d.Id).Append("\">View</a><a class=\"text-blue-600 hover:underline\" href=\"/Admin/Debts/Edit/").Append(d.Id).Append("\">Edit</a></td>");
            sb.Append("</tr>");
        }

        return Json(new { total, page, pageSize, rowsHtml = sb.ToString() });
    }
}
