using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DebtManager.Infrastructure.Persistence;

namespace DebtManager.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class DiagnosticsController : Controller
{
    private readonly AppDbContext _context;

    public DiagnosticsController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var diagnostics = new List<string>();

        // Test 1: Can connect to database
        try
        {
            var canConnect = await _context.Database.CanConnectAsync();
            diagnostics.Add($"✅ Database Connection: {(canConnect ? "SUCCESS" : "FAILED")}");
        }
        catch (Exception ex)
        {
            diagnostics.Add($"❌ Database Connection: FAILED - {ex.Message}");
        }

        // Test 2: Check if Organizations table exists
        try
        {
            var orgCount = await _context.Organizations.CountAsync();
            diagnostics.Add($"✅ Organizations table exists. Count: {orgCount}");
        }
        catch (Exception ex)
        {
            diagnostics.Add($"❌ Organizations table: FAILED - {ex.Message}");
        }

        // Test 3: Check if Debtors table exists
        try
        {
            var debtorCount = await _context.Debtors.CountAsync();
            diagnostics.Add($"✅ Debtors table exists. Count: {debtorCount}");
        }
        catch (Exception ex)
        {
            diagnostics.Add($"❌ Debtors table: FAILED - {ex.Message}");
        }

        // Test 4: Check if Debts table exists
        try
        {
            var debtCount = await _context.Debts.CountAsync();
            diagnostics.Add($"✅ Debts table exists. Count: {debtCount}");
        }
        catch (Exception ex)
        {
            diagnostics.Add($"❌ Debts table: FAILED - {ex.Message}");
        }

        // Test 5: Check if Transactions table exists
        try
        {
            var txCount = await _context.Transactions.CountAsync();
            diagnostics.Add($"✅ Transactions table exists. Count: {txCount}");
        }
        catch (Exception ex)
        {
            diagnostics.Add($"❌ Transactions table: FAILED - {ex.Message}");
        }

        // Test 6: Check dummy data
        try
        {
            var dummyOrgCount = await _context.Organizations.CountAsync(o => o.TagsCsv.Contains("dummy"));
            diagnostics.Add($"✅ Dummy Organizations: {dummyOrgCount}");
        }
        catch (Exception ex)
        {
            diagnostics.Add($"❌ Dummy Organizations query: FAILED - {ex.Message}");
        }

        // Test 7: Connection string
        diagnostics.Add($"📝 Connection String: {_context.Database.GetConnectionString()}");

        // Test 8: Pending migrations
        try
        {
            var pendingMigrations = await _context.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                diagnostics.Add($"⚠️  Pending Migrations: {string.Join(", ", pendingMigrations)}");
                diagnostics.Add("❗ Run: cd src/DebtManager.Web && dotnet ef database update");
            }
            else
            {
                diagnostics.Add("✅ All migrations applied");
            }
        }
        catch (Exception ex)
        {
            diagnostics.Add($"❌ Migration check: FAILED - {ex.Message}");
        }

        ViewBag.Diagnostics = diagnostics;
        return View();
    }
}

