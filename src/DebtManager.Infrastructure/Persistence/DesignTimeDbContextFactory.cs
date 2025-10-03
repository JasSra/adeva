using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DebtManager.Infrastructure.Persistence;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        // Default to local SQL Server; EF CLI can override via environment variables if needed
        var connectionString = Environment.GetEnvironmentVariable("DEBTMGR_CONN")
            ?? "Server=localhost,1433;Database=DebtManager;User Id=sa;Password=Your_strong_password123;TrustServerCertificate=True;Encrypt=True;";
        optionsBuilder.UseSqlServer(connectionString);
        return new AppDbContext(optionsBuilder.Options);
    }
}
