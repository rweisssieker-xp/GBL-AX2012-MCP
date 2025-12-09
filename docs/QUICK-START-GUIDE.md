# Quick Start Guide - GBL-AX2012-MCP

**Version:** 1.6.0  
**Date:** 2025-12-06

---

## 🚀 Schnellstart

### 1. Voraussetzungen

- ✅ .NET 8.0 SDK
- ✅ Microsoft Dynamics AX 2012 R3 CU13
- ✅ Windows Authentication konfiguriert
- ✅ SQL Server (für Webhooks, optional)

### 2. Projekt bauen

```powershell
cd c:\tmp\GBL-AX2012-MCP
dotnet restore
dotnet build
```

### 3. Konfiguration

**appsettings.json anpassen:**

```json
{
  "AifClient": {
    "BaseUrl": "http://ax-aos:8101/DynamicsAx/Services",
    "Company": "DAT",
    "FallbackStrategy": "auto"
  },
  "BusinessConnector": {
    "UseWrapper": true,
    "WrapperUrl": "http://localhost:8090"
  },
  "ConnectionStrings": {
    "AuditDb": "Server=localhost;Database=MCP_Audit;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

### 4. BC.Wrapper Service starten (optional)

**Nur wenn `BusinessConnector.UseWrapper = true`:**

```powershell
cd src\GBL.AX2012.MCP.BC.Wrapper
dotnet build
.\bin\Debug\net48\GBL.AX2012.MCP.BC.Wrapper.exe
```

### 5. MCP Server starten

```powershell
cd src\GBL.AX2012.MCP.Server
dotnet run
```

**Automatisch:**
- ✅ Configuration Validation
- ✅ Automatischer Fallback HTTP → NetTcp
- ✅ BC.Wrapper Integration

---

## 📋 Erste Schritte

### Health Check

```powershell
curl http://localhost:8080/health
```

### Tool aufrufen

```powershell
curl -X POST http://localhost:8080/tools/call `
  -H "Content-Type: application/json" `
  -d '{"tool": "ax_get_customer", "arguments": {"customerAccount": "CUST-001"}}'
```

### Metrics

```powershell
curl http://localhost:9090/metrics
```

---

## 🔧 Konfiguration

### AIF Client

**HTTP (Standard):**
```json
{
  "AifClient": {
    "BaseUrl": "http://ax-aos:8101/DynamicsAx/Services",
    "FallbackStrategy": "auto"
  }
}
```

**Nur NetTcp:**
```json
{
  "AifClient": {
    "BaseUrl": "http://ax-aos:8101/DynamicsAx/Services",
    "UseNetTcp": true,
    "NetTcpPort": 8201
  }
}
```

### Business Connector

**Mit Wrapper (empfohlen für .NET 8):**
```json
{
  "BusinessConnector": {
    "UseWrapper": true,
    "WrapperUrl": "http://localhost:8090"
  }
}
```

**Ohne Wrapper (nur .NET Framework):**
```json
{
  "BusinessConnector": {
    "UseWrapper": false
  }
}
```

---

## 🐛 Troubleshooting

### Problem: Configuration Validation fehlgeschlagen

**Lösung:**
- Prüfen Sie die Logs für Details
- Database Connection String korrekt?
- URLs gültig?
- Ports verfügbar?

### Problem: NetTcp Connection fehlgeschlagen

**Lösung:**
- Prüfen Sie ob NetTcp Port (8201) erreichbar ist
- Firewall-Regeln prüfen
- AX AOS NetTcp Endpoint aktiviert?

### Problem: BC.Wrapper nicht erreichbar

**Lösung:**
- BC.Wrapper Service läuft?
- Port 8090 verfügbar?
- `WrapperUrl` korrekt konfiguriert?

---

## 📚 Weitere Dokumentation

- **Setup:** `docs/BC-WRAPPER-SETUP.md`
- **NetTcp:** `docs/AIF-NETTCP-SETUP.md`
- **API Reference:** `docs/handbooks/02-API-REFERENCE.md`
- **Developer Guide:** `docs/handbooks/03-DEVELOPER-GUIDE.md`

---

**Status:** ✅ Ready for Production

