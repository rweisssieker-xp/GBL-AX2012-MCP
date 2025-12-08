# Migration Ready ✅

**Date:** 2025-12-06  
**Status:** Migration file created and ready to execute

---

## ✅ Completed

1. **Migration File Created**
   - `src/GBL.AX2012.MCP.Audit/Migrations/20251206000000_AddWebhookTables.cs`
   - Creates `WebhookSubscriptions` and `WebhookDeliveries` tables
   - Includes all indexes and foreign keys

2. **Model Snapshot Created**
   - `src/GBL.AX2012.MCP.Audit/Migrations/WebhookDbContextModelSnapshot.cs`
   - EF Core model snapshot for future migrations

3. **Build Fixed**
   - Moved `WebhookSubscription` and `WebhookRetryPolicy` to `GBL.AX2012.MCP.Core.Models`
   - Fixed all namespace references
   - All projects compile successfully

4. **Migration Scripts Created**
   - `scripts/run-migrations.ps1` (PowerShell)
   - `scripts/run-migrations.sh` (Bash)
   - `docs/DATABASE-SETUP.md` (Documentation)

---

## 🚀 Next Step: Execute Migration

### Option 1: PowerShell Script

```powershell
.\scripts\run-migrations.ps1
```

### Option 2: Bash Script

```bash
chmod +x scripts/run-migrations.sh
./scripts/run-migrations.sh
```

### Option 3: Manual Command

```bash
cd src/GBL.AX2012.MCP.Audit
dotnet ef database update --startup-project ../GBL.AX2012.MCP.Server --context WebhookDbContext
```

---

## 📋 Prerequisites

1. **EF Core Tools Installed**
   ```bash
   dotnet tool install --global dotnet-ef
   ```

2. **SQL Server Running**
   - Local SQL Server instance
   - Or connection string configured in `appsettings.json`

3. **Connection String Configured**
   ```json
   {
     "ConnectionStrings": {
       "AuditDb": "Server=localhost;Database=MCP_Audit;Trusted_Connection=True;TrustServerCertificate=True"
     }
   }
   ```

---

## ✅ Verification

After migration, verify tables exist:

```sql
USE MCP_Audit;
GO

SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME IN ('WebhookSubscriptions', 'WebhookDeliveries');
```

---

## 📝 Notes

- Migration is **idempotent** - safe to run multiple times
- Migration includes **rollback** support via `Down()` method
- All indexes and foreign keys are created automatically

---

**Last Updated:** 2025-12-06

