# Changelog - 2025-12-06

**Version:** 1.6.0

---

## 🎉 Neue Features

### BC.Wrapper Service
- ✅ .NET Framework Wrapper Service für Business Connector .NET
- ✅ REST API für BC.NET Zugriff von .NET 8
- ✅ Automatische Konfiguration
- ✅ Health Check Endpoints

### NetTcp Support für AIF
- ✅ AifNetTcpClient implementiert
- ✅ AifClientAdapter mit automatischem Fallback
- ✅ Konfigurierbare Fallback-Strategien
- ✅ Automatische URL-Konvertierung (HTTP → NetTcp)

### Configuration Validation
- ✅ Startup-Validierung aller Konfigurationen
- ✅ Database Connection Validation
- ✅ AIF/WCF Client Validation
- ✅ Business Connector Validation
- ✅ Webhook Configuration Validation
- ✅ URL Validation

---

## 🔧 Verbesserungen

### AIF Client
- ✅ Automatischer Fallback HTTP → NetTcp
- ✅ Konfigurierbare Fallback-Strategien
- ✅ Bessere Error Messages

### Business Connector
- ✅ Wrapper-basierter Zugriff für .NET 8
- ✅ Automatische Wrapper-Integration
- ✅ Fallback auf Mock (wenn Wrapper nicht verfügbar)

### Tests
- ✅ AifNetTcpClientTests
- ✅ AifClientAdapterTests
- ✅ BusinessConnectorWrapperClientTests

---

## 📚 Dokumentation

### Neue Dokumente
- ✅ `docs/BC-WRAPPER-SETUP.md` - BC.Wrapper Setup Guide
- ✅ `docs/AIF-NETTCP-SETUP.md` - NetTcp Support Guide
- ✅ `docs/QUICK-START-GUIDE.md` - Schnellstart-Anleitung
- ✅ `docs/IMPLEMENTATION-COMPLETE-2025-12-06.md` - Implementierungs-Status
- ✅ `docs/WAS-FEHLT-NOCH-AKTUELL.md` - Aktuelle Gap-Analyse
- ✅ `README-BC-WRAPPER.md` - BC.Wrapper Quick Reference

### Aktualisierte Dokumente
- ✅ `README.md` - Neue Features dokumentiert
- ✅ `docs/analysis/business-connector-net8-compatibility-2025-12-06.md` - PM-Analyse

---

## 🔄 Konfiguration

### Neue Optionen

**AifClient:**
```json
{
  "AifClient": {
    "UseNetTcp": false,
    "NetTcpPort": 8201,
    "FallbackStrategy": "auto"
  }
}
```

**BusinessConnector:**
```json
{
  "BusinessConnector": {
    "UseWrapper": true,
    "WrapperUrl": "http://localhost:8090"
  }
}
```

---

## 🐛 Bug Fixes

- ✅ Keine bekannten Bugs

---

## 📦 Neue Abhängigkeiten

- ✅ `System.ServiceModel.NetTcp` (Version 6.0.0) - Für NetTcp Support

---

## 🚀 Migration

### Von Version 1.5.0

**Keine Breaking Changes!**

**Optionale Schritte:**
1. BC.Wrapper Service installieren (wenn BC.NET benötigt)
2. NetTcp Port konfigurieren (wenn NetTcp benötigt)
3. Database Migration ausführen (für Webhooks)

---

## ✅ Vollständige Checkliste

- [x] BC.Wrapper Service implementiert
- [x] NetTcp Client implementiert
- [x] Adapter mit Fallback implementiert
- [x] Configuration Validation implementiert
- [x] Tests erstellt
- [x] Dokumentation geschrieben
- [x] README aktualisiert
- [x] Build erfolgreich

---

**Status:** ✅ Ready for Production  
**Next Version:** 1.7.0 (Performance Tests, Monitoring Dashboards)

