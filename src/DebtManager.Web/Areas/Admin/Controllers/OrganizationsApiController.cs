using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DebtManager.Infrastructure.Persistence;

namespace DebtManager.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("Admin/api/organizations")] 
public class OrganizationsApiController(AppDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? search, [FromQuery] string? status, [FromQuery] string? sortBy, [FromQuery] string? sortDir, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var q = db.Organizations.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            q = q.Where(o => o.Name.Contains(search) || (o.Subdomain ?? "").Contains(search) || o.Abn.Contains(search));
        }
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (status.Equals("active", StringComparison.OrdinalIgnoreCase)) q = q.Where(o => o.IsApproved);
            else if (status.Equals("pending", StringComparison.OrdinalIgnoreCase)) q = q.Where(o => !o.IsApproved);
        }
        if (!string.IsNullOrWhiteSpace(sortBy))
        {
            var asc = (sortDir ?? "asc").Equals("asc", StringComparison.OrdinalIgnoreCase);
            q = sortBy switch
            {
                "Name" => asc ? q.OrderBy(o => o.Name) : q.OrderByDescending(o => o.Name),
                "Abn" => asc ? q.OrderBy(o => o.Abn) : q.OrderByDescending(o => o.Abn),
                "Subdomain" => asc ? q.OrderBy(o => o.Subdomain) : q.OrderByDescending(o => o.Subdomain),
                _ => asc ? q.OrderBy(o => o.CreatedAtUtc) : q.OrderByDescending(o => o.CreatedAtUtc)
            };
        }
        var total = await q.CountAsync();
        var items = await q.Skip((page-1)*pageSize).Take(pageSize).ToListAsync();

        // Render rows server-side to keep client simple
        var sb = new StringBuilder();
        foreach (var o in items)
        {
            sb.Append("<tr class=\"hover:bg-gray-50 dark:hover:bg-gray-700\">");
            sb.Append("<td class=\"px-3 md:px-4 py-2 md:py-3\"><input type=\"checkbox\" value=\"").Append(o.Id).Append("\"></td>");
            sb.Append("<td class=\"px-3 md:px-4 py-2 md:py-3 whitespace-nowrap\">#").Append(o.Id.ToString()[..6]).Append("</td>");
            sb.Append("<td class=\"px-3 md:px-4 py-2 md:py-3 whitespace-nowrap\"><a class=\"font-medium\" href=\"/Admin/Organizations/Details/").Append(o.Id).Append("\">").Append(System.Net.WebUtility.HtmlEncode(o.Name)).Append("</a></td>");
            sb.Append("<td class=\"px-3 md:px-4 py-2 md:py-3 whitespace-nowrap\">").Append(System.Net.WebUtility.HtmlEncode(o.Abn)).Append("</td>");
            sb.Append("<td class=\"px-3 md:px-4 py-2 md:py-3 whitespace-nowrap\">").Append(System.Net.WebUtility.HtmlEncode(o.Subdomain ?? "")).Append("</td>");
            sb.Append("<td class=\"px-3 md:px-4 py-2 md:py-3 whitespace-nowrap\">")
              .Append(o.IsApproved ? "<span class=\\\"px-2 py-1 text-xs rounded-full bg-green-100 text-green-800\\\">Active</span>" : "<span class=\\\"px-2 py-1 text-xs rounded-full bg-yellow-100 text-yellow-800\\\">Pending</span>")
              .Append("</td>");
            sb.Append("<td class=\"px-3 md:px-4 py-2 md:py-3 text-right whitespace-nowrap\"><a class=\"text-blue-600 hover:underline mr-3\" href=\"/Admin/Organizations/Details/").Append(o.Id).Append("\">View</a><a class=\"text-blue-600 hover:underline\" href=\"/Admin/Organizations/Edit/").Append(o.Id).Append("\">Edit</a></td>");
            sb.Append("</tr>");
        }

        return Json(new { total, page, pageSize, rowsHtml = sb.ToString() });
    }
}
