# EF Core Migrations

To add initial migration once .NET SDK is available:

1. Install EF tools (global):
   - dotnet tool install --global dotnet-ef
2. Add migration:
   - dotnet ef migrations add InitialCreate -p src/DebtManager.Infrastructure -s src/DebtManager.Web
3. Update database:
   - dotnet ef database update -p src/DebtManager.Infrastructure -s src/DebtManager.Web
