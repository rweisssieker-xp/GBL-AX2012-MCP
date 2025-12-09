# Datenbank-Erklärung

**Date:** 2025-12-06

---

## Welche Datenbank?

Es gibt **eine SQL Server Datenbank** namens **`MCP_Audit`**, die für zwei verschiedene Zwecke verwendet wird:

### 1. Audit Logging (AuditDbContext)
- **Tabelle:** `AuditEntries`
- **Zweck:** Speichert alle MCP Tool-Aufrufe für Audit-Zwecke
- **Status:** ✅ Bereits implementiert (EfCoreAuditService)

### 2. Webhooks (WebhookDbContext)
- **Tabellen:** 
  - `WebhookSubscriptions` - Webhook-Abonnements
  - `WebhookDeliveries` - Webhook-Lieferhistorie
- **Zweck:** Speichert Webhook-Konfigurationen und Lieferhistorie
- **Status:** ⚠️ Migration erstellt, aber noch nicht ausgeführt

---

## Connection String

**Eine Datenbank, zwei DbContexts:**

```json
{
  "ConnectionStrings": {
    "AuditDb": "Server=localhost;Database=MCP_Audit;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

**Beide DbContexts verwenden die gleiche Connection String:**
- `AuditDbContext` → `AuditDb` Connection String
- `WebhookDbContext` → `AuditDb` Connection String

**Warum?**
- Beide gehören zum Audit/Monitoring-Bereich
- Einfacheres Management
- Kann später getrennt werden, wenn nötig

---

## Aktuelle Situation

### ✅ Was funktioniert:
- `AuditDbContext` - Audit-Einträge werden gespeichert
- Migration `20251206000000_AddWebhookTables.cs` ist erstellt

### ⚠️ Was noch fehlt:
- Migration muss ausgeführt werden, um Webhook-Tabellen zu erstellen
- Ohne Migration funktionieren Webhooks nicht (keine Tabellen)

---

## Migration ausführen

Die Migration erstellt die Webhook-Tabellen in der **gleichen Datenbank** (`MCP_Audit`):

```powershell
cd src/GBL.AX2012.MCP.Audit
dotnet ef database update --startup-project ../GBL.AX2012.MCP.Server --context WebhookDbContext
```

**Oder mit Script:**
```powershell
.\scripts\run-migrations.ps1
```

---

## Nach Migration: Datenbank-Struktur

```
MCP_Audit (SQL Server Database)
├── AuditEntries (von AuditDbContext)
│   ├── Id
│   ├── Timestamp
│   ├── UserId
│   ├── ToolName
│   └── ...
│
└── WebhookSubscriptions (von WebhookDbContext)
    ├── Id
    ├── EventType
    ├── WebhookUrl
    └── ...
│
└── WebhookDeliveries (von WebhookDbContext)
    ├── Id
    ├── SubscriptionId
    ├── Payload
    └── ...
```

---

## Zusammenfassung

**Eine Datenbank:** `MCP_Audit`  
**Zwei DbContexts:**
1. `AuditDbContext` → `AuditEntries` Tabelle
2. `WebhookDbContext` → `WebhookSubscriptions` + `WebhookDeliveries` Tabellen

**Migration nötig:** Ja, für Webhook-Tabellen

---

**Last Updated:** 2025-12-06

