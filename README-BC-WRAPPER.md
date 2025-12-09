# BC.Wrapper - Business Connector Wrapper für .NET 8

## Übersicht

Der **BC.Wrapper** ist ein separater .NET Framework Service, der Business Connector .NET (BC.NET) für .NET Core 8 / .NET 8 zugänglich macht.

**Problem:** BC.NET ist eine .NET Framework COM-Komponente und funktioniert nicht direkt mit .NET 8.

**Lösung:** Ein separater .NET Framework Service, der BC.NET verwendet und über HTTP REST API erreichbar ist.

## Architektur

```
┌─────────────────────┐         HTTP          ┌──────────────────────┐         BC.NET         ┌──────────┐
│  .NET 8 MCP Server │ ────────────────────> │ .NET Framework       │ ────────────────────> │ AX 2012  │
│                    │                        │ BC.Wrapper Service   │                        │          │
└────────────────────┘                        └──────────────────────┘                        └──────────┘
```

## Quick Start

### 1. BC.Wrapper Service starten

```powershell
cd src\GBL.AX2012.MCP.BC.Wrapper
dotnet build
cd bin\Debug\net48
.\GBL.AX2012.MCP.BC.Wrapper.exe
```

### 2. MCP Server konfigurieren

**appsettings.json:**
```json
{
  "BusinessConnector": {
    "UseWrapper": true,
    "WrapperUrl": "http://localhost:8090",
    "ObjectServer": "ax-aos:2712",
    "Company": "DAT",
    "Language": "en-us"
  }
}
```

### 3. MCP Server starten

```powershell
cd src\GBL.AX2012.MCP.Server
dotnet run
```

## API Endpoints

### Health Check
```powershell
POST http://localhost:8090/api/health/check
Content-Type: application/json

{
  "Company": "DAT",
  "ObjectServer": "ax-aos:2712",
  "Language": "en-us"
}
```

### Status
```powershell
GET http://localhost:8090/api/health/status
```

## Weitere Informationen

Siehe: `docs/BC-WRAPPER-SETUP.md`

