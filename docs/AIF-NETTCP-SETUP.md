# AIF NetTcp Support - Setup Guide

**Date:** 2025-12-06  
**Purpose:** Unterstützung für AX 2012 AIF mit NetTcpBinding (wenn HTTP nicht erlaubt)

---

## Problem

AX 2012 erlaubt manchmal nur **NetTcpBinding** für AIF-Kommunikation, nicht HTTP. Der Standard `AifClient` verwendet HTTP und funktioniert dann nicht.

## Lösung

Automatischer Fallback-Mechanismus:
1. **HTTP versuchen** (Standard)
2. **Bei Fehler automatisch auf NetTcp wechseln**
3. **NetTcp als bevorzugte Methode merken**

---

## Architektur

```
┌─────────────────┐
│  AifClientAdapter │
│  (Fallback Logic) │
└────────┬──────────┘
         │
    ┌────┴────┐
    │         │
┌───▼───┐ ┌──▼──────┐
│ HTTP  │ │ NetTcp  │
│ Client│ │ Client  │
└───────┘ └─────────┘
```

---

## Konfiguration

### appsettings.json

```json
{
  "AifClient": {
    "BaseUrl": "http://ax-aos:8101/DynamicsAx/Services",
    "Timeout": "00:00:30",
    "Company": "DAT",
    "UseNetTcp": false,
    "NetTcpPort": 8201,
    "FallbackStrategy": "auto"
  }
}
```

### FallbackStrategy Optionen

| Wert | Verhalten |
|------|-----------|
| `"auto"` | Versucht HTTP zuerst, bei Fehler automatisch NetTcp (Standard) |
| `"http"` | Nur HTTP verwenden (kein Fallback) |
| `"nettcp"` | Nur NetTcp verwenden (kein Fallback) |

### UseNetTcp

Wenn `UseNetTcp: true`, wird direkt NetTcp verwendet (kein Fallback).

---

## URL-Konvertierung

Der Adapter konvertiert automatisch HTTP URLs zu NetTcp URLs:

**HTTP:**
```
http://ax-aos:8101/DynamicsAx/Services/CustCustomerService
```

**NetTcp:**
```
net.tcp://ax-aos:8201/DynamicsAx/Services/CustCustomerService
```

**Port-Mapping:**
- HTTP Port: 8101 (Standard AIF HTTP)
- NetTcp Port: 8201 (Standard AIF NetTcp, konfigurierbar)

---

## Verwendung

### Automatischer Fallback (Standard)

```json
{
  "AifClient": {
    "FallbackStrategy": "auto"
  }
}
```

**Verhalten:**
1. Erster Request: HTTP versuchen
2. Bei Fehler: Automatisch NetTcp verwenden
3. Weitere Requests: NetTcp verwenden (wird gemerkt)

### Nur NetTcp

```json
{
  "AifClient": {
    "UseNetTcp": true,
    "NetTcpPort": 8201
  }
}
```

**Verhalten:**
- Direkt NetTcp verwenden, kein Fallback

### Nur HTTP

```json
{
  "AifClient": {
    "FallbackStrategy": "http"
  }
}
```

**Verhalten:**
- Nur HTTP verwenden, kein Fallback

---

## Logging

Der Adapter loggt automatisch:

```
[Debug] Using HTTP client for GetCustomer
[Warning] HTTP failed for GetCustomer, trying NetTcp fallback
[Information] Switching to NetTcp for GetCustomer due to HTTP failure
[Debug] Using NetTcp client for GetCustomer
```

---

## Troubleshooting

### Problem: NetTcp Connection fehlgeschlagen

**Mögliche Ursachen:**
1. NetTcp Port nicht erreichbar (Firewall)
2. AX AOS NetTcp Endpoint nicht aktiviert
3. Falscher Port konfiguriert

**Lösung:**
```json
{
  "AifClient": {
    "NetTcpPort": 8201  // Prüfen ob Port korrekt ist
  }
}
```

### Problem: Beide Methoden schlagen fehl

**Lösung:**
- Prüfen ob AX AOS erreichbar ist
- Prüfen ob AIF Services aktiviert sind
- Windows Authentication funktioniert?

### Problem: Performance

**NetTcp vs HTTP:**
- NetTcp ist normalerweise schneller (binär, weniger Overhead)
- HTTP ist einfacher zu debuggen (SOAP XML sichtbar)

---

## Testing

### 1. HTTP Test

```json
{
  "AifClient": {
    "FallbackStrategy": "http"
  }
}
```

### 2. NetTcp Test

```json
{
  "AifClient": {
    "UseNetTcp": true
  }
}
```

### 3. Auto Fallback Test

```json
{
  "AifClient": {
    "FallbackStrategy": "auto"
  }
}
```

**Erwartetes Verhalten:**
- HTTP funktioniert → HTTP verwenden
- HTTP fehlgeschlagen → NetTcp verwenden
- Beide funktionieren → HTTP verwenden (schnellerer Start)

---

## Port-Konfiguration

### Standard Ports

| Service | HTTP Port | NetTcp Port |
|---------|-----------|-------------|
| AIF | 8101 | 8201 |
| Custom WCF | 8102 | 8202 |

### Port ändern

```json
{
  "AifClient": {
    "BaseUrl": "http://ax-aos:8101/DynamicsAx/Services",
    "NetTcpPort": 8201  // Anpassen falls nötig
  }
}
```

---

## Security

### Windows Authentication

Beide Clients (HTTP und NetTcp) verwenden Windows Authentication:
- HTTP: `UseDefaultCredentials = true`
- NetTcp: `TcpClientCredentialType.Windows`

### Firewall

**NetTcp benötigt:**
- Port 8201 (oder konfigurierter Port) offen
- Zwischen MCP Server und AX AOS

---

## Performance

### Vergleich

| Metrik | HTTP | NetTcp |
|--------|------|--------|
| Overhead | Höher (XML) | Niedriger (binär) |
| Geschwindigkeit | Langsamer | Schneller |
| Debugging | Einfacher | Schwerer |
| Firewall | Einfacher | Komplexer |

### Empfehlung

- **Development:** HTTP (einfacher zu debuggen)
- **Production:** NetTcp (bessere Performance)
- **Auto:** Beste Kompatibilität (funktioniert immer)

---

## Weitere Informationen

- [AX 2012 AIF Documentation](https://docs.microsoft.com/en-us/dynamicsax-2012/appuser-itpro/application-integration-framework-aif)
- [WCF NetTcpBinding](https://docs.microsoft.com/en-us/dotnet/framework/wcf/feature-details/nettcpbinding)

---

**Status:** ✅ Ready for Production  
**Default:** Auto-Fallback aktiviert

