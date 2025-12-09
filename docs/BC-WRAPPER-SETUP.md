# BC.Wrapper Service - Setup Guide

**Date:** 2025-12-06  
**Purpose:** Enable Business Connector .NET access from .NET Core 8

---

## Übersicht

Der **BC.Wrapper** ist ein separater .NET Framework Service, der:
- Business Connector .NET verwendet (läuft auf .NET Framework 4.8)
- Über HTTP REST API erreichbar ist
- Von .NET 8 MCP Server aufgerufen wird

**Architektur:**
```
.NET 8 MCP Server → HTTP → .NET Framework BC.Wrapper → BC.NET → AX 2012
```

---

## Voraussetzungen

1. **.NET Framework 4.8** installiert
2. **Business Connector .NET** installiert (Teil von AX 2012 Client)
3. **AX 2012 AOS** erreichbar
4. **Port 8090** verfügbar (oder konfigurierbar)

---

## Installation

### 1. Projekt bauen

```powershell
cd src\GBL.AX2012.MCP.BC.Wrapper
dotnet build
```

### 2. BC.NET DLL Referenz prüfen

Die BC.NET DLL sollte im GAC oder unter folgendem Pfad verfügbar sein:
```
C:\Program Files (x86)\Microsoft Dynamics AX\60\BusinessConnector\Bin\Microsoft.Dynamics.BusinessConnectorNet.dll
```

Falls nicht im GAC, muss die DLL manuell referenziert werden.

### 3. Service starten

```powershell
cd src\GBL.AX2012.MCP.BC.Wrapper\bin\Debug\net48
.\GBL.AX2012.MCP.BC.Wrapper.exe
```

**Oder mit Port-Konfiguration:**
```powershell
$env:BC_WRAPPER_PORT="8090"
.\GBL.AX2012.MCP.BC.Wrapper.exe
```

---

## Konfiguration

### BC.Wrapper Service

**app.config:**
```xml
<appSettings>
  <add key="BC_WRAPPER_PORT" value="8090" />
</appSettings>
```

**Oder Environment Variable:**
```powershell
$env:BC_WRAPPER_PORT="8090"
```

### MCP Server

**appsettings.json:**
```json
{
  "BusinessConnector": {
    "ObjectServer": "ax-aos:2712",
    "Company": "DAT",
    "Language": "en-us",
    "UseWrapper": true,
    "WrapperUrl": "http://localhost:8090"
  }
}
```

**Oder Environment Variable:**
```powershell
$env:BC_WRAPPER_URL="http://localhost:8090"
```

---

## API Endpoints

### Health Check

**POST** `/api/health/check`

**Request:**
```json
{
  "Company": "DAT",
  "ObjectServer": "ax-aos:2712",
  "Language": "en-us",
  "Configuration": null
}
```

**Response:**
```json
{
  "Status": "healthy",
  "AosConnected": true,
  "ResponseTimeMs": 123,
  "Timestamp": "2025-12-06T14:30:00Z",
  "Details": {
    "database": "connected",
    "business_connector": "connected",
    "company": "DAT",
    "aos": "ax-aos:2712"
  },
  "Error": null
}
```

### Status

**GET** `/api/health/status`

**Response:**
```json
{
  "service": "BC.Wrapper",
  "status": "running",
  "connected": true,
  "timestamp": "2025-12-06T14:30:00Z"
}
```

---

## Testing

### 1. Service Status prüfen

```powershell
curl http://localhost:8090/api/health/status
```

### 2. Health Check testen

```powershell
$body = @{
    Company = "DAT"
    ObjectServer = "ax-aos:2712"
    Language = "en-us"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:8090/api/health/check" `
    -Method Post `
    -Body $body `
    -ContentType "application/json"
```

### 3. MCP Server testen

```powershell
# MCP Server starten (verwendet automatisch BC.Wrapper)
cd src\GBL.AX2012.MCP.Server
dotnet run
```

---

## Windows Service (Optional)

Für Production kann der BC.Wrapper als Windows Service installiert werden:

### 1. NSSM verwenden (Non-Sucking Service Manager)

```powershell
# Download NSSM: https://nssm.cc/download
nssm install BCWrapper "C:\Path\To\GBL.AX2012.MCP.BC.Wrapper.exe"
nssm set BCWrapper AppEnvironmentExtra BC_WRAPPER_PORT=8090
nssm start BCWrapper
```

### 2. Oder TopShelf verwenden

Siehe: https://github.com/Topshelf/Topshelf

---

## Troubleshooting

### Problem: BC.NET DLL nicht gefunden

**Lösung:**
1. Prüfen ob BC.NET installiert ist
2. DLL-Pfad in `.csproj` anpassen
3. DLL in `bin` Ordner kopieren

### Problem: Port bereits belegt

**Lösung:**
```powershell
# Anderen Port verwenden
$env:BC_WRAPPER_PORT="8091"
```

### Problem: Connection zu AX fehlgeschlagen

**Lösung:**
1. AOS erreichbar? `ping ax-aos`
2. Firewall-Regeln prüfen
3. Windows Authentication funktioniert?
4. Company Name korrekt?

### Problem: MCP Server findet Wrapper nicht

**Lösung:**
1. Wrapper läuft? `curl http://localhost:8090/api/health/status`
2. `WrapperUrl` in `appsettings.json` korrekt?
3. Firewall-Regeln für Port 8090?

---

## Performance

- **Overhead:** ~10-20ms zusätzliche Latenz durch HTTP
- **Concurrency:** Wrapper unterstützt mehrere gleichzeitige Requests
- **Connection Pooling:** BC.NET Connection wird wiederverwendet

---

## Security

- **Lokaler Service:** Standardmäßig nur `localhost` erreichbar
- **Production:** Firewall-Regeln für Port 8090
- **Authentication:** Optional HTTP Basic Auth hinzufügen

---

## Monitoring

### Logs

Logs werden in Console ausgegeben. Für Production:
- Logging zu Datei konfigurieren
- Oder Application Insights verwenden

### Health Checks

- `/api/health/status` - Service Status
- `/api/health/check` - AX Connectivity

---

## Deployment

### Development

1. BC.Wrapper manuell starten
2. MCP Server starten (verwendet Wrapper automatisch)

### Production

1. BC.Wrapper als Windows Service installieren
2. MCP Server als Windows Service installieren
3. Beide Services konfigurieren
4. Health Checks einrichten

---

## Weitere Entwicklung

### Erweiterte Features

- **Query Execution:** X++ Queries ausführen
- **Record Operations:** CRUD Operations
- **Batch Operations:** Mehrere Operationen gleichzeitig
- **Caching:** Ergebnisse cachen

### Beispiel: Query Execution

```csharp
// In BusinessConnectorService.cs
public QueryResult ExecuteQuery(string query)
{
    // Implementierung
}
```

---

**Status:** ✅ Ready for Development  
**Next Steps:** Testing in Development Environment

