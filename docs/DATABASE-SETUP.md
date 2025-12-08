# Database Setup Guide

**Date:** 2025-12-06  
**Purpose:** Setup database for Webhook Subscriptions

---

## Prerequisites

1. SQL Server installed and running
2. .NET 8.0 SDK installed
3. EF Core Tools installed

---

## Step 1: Install EF Core Tools

```bash
dotnet tool install --global dotnet-ef
```

Verify installation:
```bash
dotnet ef --version
```

---

## Step 2: Configure Connection String

Update `src/GBL.AX2012.MCP.Server/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "AuditDb": "Server=localhost;Database=MCP_Audit;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

**For Production:**
- Use environment variables
- Use Azure Key Vault
- Use User Secrets for development

---

## Step 3: Create Database (if not exists)

```sql
CREATE DATABASE MCP_Audit;
GO

USE MCP_Audit;
GO
```

---

## Step 4: Run Migration

### Option A: Using PowerShell Script

```powershell
.\scripts\run-migrations.ps1
```

### Option B: Using Bash Script

```bash
chmod +x scripts/run-migrations.sh
./scripts/run-migrations.sh
```

### Option C: Manual Command

```bash
cd src/GBL.AX2012.MCP.Audit
dotnet ef database update --startup-project ../GBL.AX2012.MCP.Server --context WebhookDbContext
```

---

## Step 5: Verify Tables Created

```sql
USE MCP_Audit;
GO

-- Check tables exist
SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME IN ('WebhookSubscriptions', 'WebhookDeliveries');

-- Check indexes
SELECT * FROM sys.indexes 
WHERE object_id IN (
    OBJECT_ID('WebhookSubscriptions'),
    OBJECT_ID('WebhookDeliveries')
);
```

---

## Expected Tables

### WebhookSubscriptions

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier | Primary key |
| EventType | nvarchar(128) | Event type to subscribe to |
| WebhookUrl | nvarchar(512) | URL to deliver webhook |
| Secret | nvarchar(256) | HMAC secret (optional) |
| Filters | nvarchar(max) | JSON filters |
| MaxRetries | int | Max retry attempts |
| BackoffMs | int | Retry backoff in ms |
| ExponentialBackoff | bit | Use exponential backoff |
| IsActive | bit | Subscription active flag |
| CreatedAt | datetime2 | Creation timestamp |
| LastTriggeredAt | datetime2 | Last trigger time |
| SuccessCount | int | Successful deliveries |
| FailureCount | int | Failed deliveries |

### WebhookDeliveries

| Column | Type | Description |
|--------|------|-------------|
| Id | uniqueidentifier | Primary key |
| SubscriptionId | uniqueidentifier | FK to WebhookSubscriptions |
| EventType | nvarchar(128) | Event type |
| Payload | nvarchar(max) | Webhook payload (JSON) |
| Status | nvarchar(32) | pending/delivered/failed |
| Attempt | int | Retry attempt number |
| HttpStatusCode | int | HTTP response code |
| ErrorMessage | nvarchar(4000) | Error message if failed |
| DeliveredAt | datetime2 | Delivery timestamp |
| CompletedAt | datetime2 | Completion timestamp |

---

## Troubleshooting

### Error: "Cannot find DbContext"

**Solution:** Ensure WebhookDbContext is in the correct namespace and referenced.

### Error: "Connection string not found"

**Solution:** Check appsettings.json has "ConnectionStrings:AuditDb" configured.

### Error: "Migration already exists"

**Solution:** Migration file exists. Just run `dotnet ef database update`.

---

## Rollback Migration

If you need to rollback:

```bash
cd src/GBL.AX2012.MCP.Audit
dotnet ef database update 0 --startup-project ../GBL.AX2012.MCP.Server --context WebhookDbContext
```

---

## Next Steps

After migration:

1. ✅ Verify tables created
2. ✅ Test webhook subscription: `ax_subscribe_webhook`
3. ✅ Test webhook delivery
4. ✅ Check delivery history in `WebhookDeliveries` table

---

**Last Updated:** 2025-12-06

