# Migration Hinweis

**Date:** 2025-12-06

---

## ⚠️ Wichtig: SQL Server erforderlich

Die Migration kann nur ausgeführt werden, wenn:
1. SQL Server installiert und laufend ist
2. Die Datenbank `MCP_Audit` existiert (oder erstellt wird)
3. Die Connection String in `appsettings.json` korrekt ist

---

## Aktueller Status

✅ **Migration vorbereitet:**
- ✅ `WebhookDbContextFactory.cs` erstellt
- ✅ `Microsoft.EntityFrameworkCore.Design` zum Server-Projekt hinzugefügt
- ✅ `Microsoft.Extensions.Configuration.Json` zum Audit-Projekt hinzugefügt
- ✅ Migration `20251206000000_AddWebhookTables.cs` existiert

⚠️ **Migration noch nicht ausgeführt:**
- SQL Server nicht verfügbar oder nicht erreichbar
- Datenbank `MCP_Audit` muss erstellt werden

---

## Migration ausführen (wenn SQL Server verfügbar)

### Option 1: Datenbank zuerst erstellen

```sql
CREATE DATABASE MCP_Audit;
GO
```

Dann Migration ausführen:
```powershell
cd src\GBL.AX2012.MCP.Audit
dotnet ef database update --startup-project ..\GBL.AX2012.MCP.Server --context WebhookDbContext
```

### Option 2: Mit Script

```powershell
.\scripts\run-migrations.ps1
```

---

## Connection String anpassen

Falls SQL Server auf einem anderen Server läuft, `appsettings.json` anpassen:

```json
{
  "ConnectionStrings": {
    "AuditDb": "Server=YOUR_SERVER;Database=MCP_Audit;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

Oder mit SQL Authentication:
```json
{
  "ConnectionStrings": {
    "AuditDb": "Server=YOUR_SERVER;Database=MCP_Audit;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True"
  }
}
```

---

## Alternative: InMemory für Tests

Für Tests wird eine InMemory-Datenbank verwendet, daher funktionieren Tests auch ohne SQL Server.

---

## Nächste Schritte

1. SQL Server installieren/starten (falls nicht vorhanden)
2. Datenbank `MCP_Audit` erstellen
3. Migration ausführen
4. Webhooks testen

---

**Last Updated:** 2025-12-06

