# Scenarios Page Troubleshooting Guide

## Issue: Scenarios Buttons Don't Work

If you visit `https://localhost:5001/Admin/Scenarios` and the buttons (Create, Generate Pack, etc.) don't do anything and you don't see organizations, debtors, or transactions in the database, the most likely cause is:

**The SQL Server database is not running.**

## Solution

### 1. Start SQL Server

Run this command from the project root:

```bash
cd deploy && docker compose up -d
```

This will start the SQL Server container on `localhost:1433` with the credentials specified in `appsettings.json`:
- Username: `sa`
- Password: `Your_strong_password123`

### 2. Verify Database is Running

Check that the container is running:

```bash
docker ps | grep sql
```

You should see a container named `debtmanager-sql`.

### 3. Refresh the Scenarios Page

Navigate to `https://localhost:5001/Admin/Scenarios` and you should now see:
- ✅ **Green banner**: "Database Connected - SQL Server on localhost:1433"
- All buttons should work properly
- Data will be created and visible in the database

## Visual Indicators

The Scenarios page now includes visual feedback:

- **🟢 Green Banner**: Database is connected and working
- **🔴 Red Banner**: Database is NOT connected - shows instructions to start it
- **Error Messages**: If buttons are clicked while database is down, you'll see clear error messages

## Testing the Fix

After starting SQL Server:

1. Visit `/Admin/Scenarios`
2. Click "Create" in the "Quick Debtor Case" section
3. Check the success message - it should show created organization, debtor, and debt
4. Click "View Organizations" to see the newly created data
5. The stats should update showing counts of Organizations, Debtors, Debts, etc.

## Common Issues

### Docker Not Installed/Running
If you don't have Docker installed or the Docker daemon isn't running:
- Install Docker Desktop for macOS
- Start Docker Desktop
- Run the command again

### Port 1433 Already in Use
If another SQL Server instance is using port 1433:
```bash
lsof -i :1433
# Kill the process or change the port in docker-compose.yml and appsettings.json
```

### Database Migration Issues
If the database schema is not up to date:
```bash
cd src/DebtManager.Web
dotnet ef database update
```

## Code Changes Made

The following improvements were added to prevent silent failures:

1. **Database connectivity check** - All POST actions now check if the database is reachable before attempting operations
2. **Clear error messages** - If database is down, users see actionable error messages with the exact command to run
3. **Visual status indicator** - The page shows database connection status immediately
4. **Better exception handling** - Errors include stack traces for debugging

## Related Files

- Controller: `/src/DebtManager.Web/Areas/Admin/Controllers/ScenariosController.cs`
- View: `/src/DebtManager.Web/Areas/Admin/Views/Scenarios/Index.cshtml`
- Docker Compose: `/deploy/docker-compose.yml`
- Connection String: `/src/DebtManager.Web/appsettings.json`

