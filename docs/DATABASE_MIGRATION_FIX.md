# Database Migration Fix - Communications Tables

## Issue
```
Microsoft.Data.SqlClient.SqlException: Invalid object name 'MessageTemplates'.
```

The application was trying to seed message templates but the database tables didn't exist yet.

## Solution Applied

### 1. ? Installed EF Core Tools
```powershell
dotnet tool install --global dotnet-ef
```

### 2. ? Created Migration
```powershell
cd src/DebtManager.Infrastructure
dotnet ef migrations add AddCommunicationsTables --startup-project ../DebtManager.Web
```

This created a new migration file in `src/DebtManager.Infrastructure/Migrations/` with the SQL to create:
- `MessageTemplates` table
- `QueuedMessages` table
- `InternalMessages` table
- `InternalMessageRecipients` table

### 3. ? Automatic Application
The migration will be applied automatically when you start the application because `DbInitializer.cs` line 20 contains:

```csharp
await db.Database.MigrateAsync();
```

This runs **every time the application starts** and applies any pending migrations.

## What Tables Will Be Created

### MessageTemplates
- Stores email and SMS templates with Handlebars syntax
- Used by: Organization onboarding, payment reminders, etc.

### QueuedMessages
- Stores outgoing emails and SMS messages
- Tracks delivery status, retries, errors

### InternalMessages
- System-generated notifications for admins
- Priority levels (Low, Normal, High, Urgent)
- Categories for filtering

### InternalMessageRecipients
- Links internal messages to admin users
- Tracks read/unread status per recipient

## Next Steps

### Just Run the Application!
```powershell
cd src/DebtManager.Web
dotnet run
```

**What will happen:**
1. ? App starts
2. ? `DbInitializer` runs
3. ? `MigrateAsync()` applies the new migration
4. ? Tables are created
5. ? Message templates are seeded
6. ? App continues to start normally

### If You See the Maintenance Page
If the migration fails and you see the maintenance page:

1. **Check the error details** (shown in development mode)
2. **Common issues:**
   - LocalDB not started ? Start SQL Server (LocalDB) service
   - Database permissions ? LocalDB should work automatically
   - Connection string ? Already correctly configured

### Verify Tables Were Created

After the app starts successfully, you can check:

```sql
-- In SQL Server Management Studio or Azure Data Studio
SELECT * FROM MessageTemplates;
SELECT * FROM QueuedMessages;
SELECT * FROM InternalMessages;
SELECT * FROM InternalMessageRecipients;
```

## Migration Details

### Created Files
- `src/DebtManager.Infrastructure/Migrations/[timestamp]_AddCommunicationsTables.cs`
- `src/DebtManager.Infrastructure/Migrations/[timestamp]_AddCommunicationsTables.Designer.cs`
- `src/DebtManager.Infrastructure/Migrations/AppDbContextModelSnapshot.cs` (updated)

### Tables Schema

**MessageTemplates:**
```sql
CREATE TABLE MessageTemplates (
    Id uniqueidentifier PRIMARY KEY,
    Code nvarchar(100) NOT NULL UNIQUE,
    Name nvarchar(200) NOT NULL,
    Subject nvarchar(500) NULL,
    BodyTemplate nvarchar(max) NOT NULL,
    Channel int NOT NULL,
    IsActive bit NOT NULL,
    Description nvarchar(1000) NULL,
    CreatedAtUtc datetime2 NOT NULL,
    UpdatedAtUtc datetime2 NOT NULL
);
```

**QueuedMessages:**
```sql
CREATE TABLE QueuedMessages (
    Id uniqueidentifier PRIMARY KEY,
    RecipientEmail nvarchar(256) NULL,
    RecipientPhone nvarchar(50) NULL,
    Subject nvarchar(500) NOT NULL,
    Body nvarchar(max) NOT NULL,
    Channel int NOT NULL,
    Status int NOT NULL,
    QueuedAtUtc datetime2 NOT NULL,
    SentAtUtc datetime2 NULL,
    FailedAtUtc datetime2 NULL,
    ErrorMessage nvarchar(2000) NULL,
    RetryCount int NOT NULL,
    RelatedEntityType nvarchar(100) NULL,
    RelatedEntityId uniqueidentifier NULL,
    ProviderMessageId nvarchar(200) NULL,
    CreatedAtUtc datetime2 NOT NULL,
    UpdatedAtUtc datetime2 NOT NULL
);
```

**InternalMessages:**
```sql
CREATE TABLE InternalMessages (
    Id uniqueidentifier PRIMARY KEY,
    Title nvarchar(300) NOT NULL,
    Content nvarchar(max) NOT NULL,
    Priority int NOT NULL,
    Category nvarchar(100) NULL,
    SenderId uniqueidentifier NULL,
    SentAtUtc datetime2 NOT NULL,
    RelatedEntityType nvarchar(100) NULL,
    RelatedEntityId uniqueidentifier NULL,
    IsSystemGenerated bit NOT NULL,
    CreatedAtUtc datetime2 NOT NULL,
    UpdatedAtUtc datetime2 NOT NULL
);
```

**InternalMessageRecipients:**
```sql
CREATE TABLE InternalMessageRecipients (
    Id uniqueidentifier PRIMARY KEY,
    InternalMessageId uniqueidentifier NOT NULL,
    UserId uniqueidentifier NOT NULL,
    Status int NOT NULL,
    ReadAtUtc datetime2 NULL,
    ArchivedAtUtc datetime2 NULL,
    CreatedAtUtc datetime2 NOT NULL,
    UpdatedAtUtc datetime2 NOT NULL,
    CONSTRAINT FK_InternalMessageRecipients_InternalMessages 
        FOREIGN KEY (InternalMessageId) 
        REFERENCES InternalMessages(Id) 
        ON DELETE CASCADE
);
```

### Indexes Created
- MessageTemplates: Unique index on `Code`
- QueuedMessages: Index on `Status`, `QueuedAtUtc`, composite on `RelatedEntityType + RelatedEntityId`
- InternalMessages: Index on `SentAtUtc`, `Priority`, composite on `RelatedEntityType + RelatedEntityId`
- InternalMessageRecipients: Composite index on `UserId + Status`, index on `InternalMessageId`

## Troubleshooting

### Issue: LocalDB Not Running
**Solution:**
```powershell
sqllocaldb start MSSQLLocalDB
```

### Issue: Database Doesn't Exist
**Solution:**
The migration will create it automatically. No action needed.

### Issue: Migration Already Applied
**Solution:**
EF Core tracks migrations in `__EFMigrationsHistory` table. It won't re-apply migrations that are already applied.

### Issue: Need to Rollback
**Solution:**
```powershell
cd src/DebtManager.Infrastructure
dotnet ef database update [PreviousMigrationName] --startup-project ../DebtManager.Web
```

### Issue: Want to Remove Migration
**Solution:**
```powershell
cd src/DebtManager.Infrastructure
dotnet ef migrations remove --startup-project ../DebtManager.Web
```

## Summary

? **Migration Created:** `AddCommunicationsTables`
? **Location:** `src/DebtManager.Infrastructure/Migrations/`
? **Will Apply:** Automatically on next app startup
? **Creates:** 4 tables for messaging system
? **Seeds:** Message templates for onboarding

**Next:** Just run `dotnet run` and the migration will apply automatically! ??

---

**Status:** ? Ready
**Action Required:** None - just start the app
**Expected Result:** Tables created, templates seeded, app runs normally
