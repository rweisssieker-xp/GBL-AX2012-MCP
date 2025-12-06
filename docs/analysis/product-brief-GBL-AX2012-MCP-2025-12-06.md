---
stepsCompleted: [1, 2, 3, 4]
inputDocuments:
  - "docs/analysis/brainstorming-session-2025-12-06.md"
workflowType: "product-brief"
lastStep: 2
project_name: "GBL-AX2012-MCP"
user_name: "Reinerw"
date: "2025-12-06"
---

# Product Brief: GBL-AX2012-MCP

**Date:** 2025-12-06
**Author:** Reinerw

---

## Executive Summary

**GBL-AX2012-MCP** ist ein Model Context Protocol (MCP) Server, der Microsoft Dynamics AX 2012 R3 für AI-gestützte Automatisierung öffnet. Das Ziel: **Vollständige Order-to-Cash Automatisierung** durch die Kombination von LLM-Intelligence, n8n-Orchestrierung und sicheren AX-Operationen.

Der MCP-Server fungiert als sichere Execution-Layer zwischen AI/Automation-Tools und dem ERP-System. Er ermöglicht es, manuelle Prozesse — von der Auftragserfassung über Fulfillment und Invoicing bis zum Zahlungseingang — durch intelligente Agenten zu automatisieren, während Governance, Audit und Sicherheit gewährleistet bleiben.

**Kernversprechen:** Was heute Sachbearbeiter manuell im AX-Client tun, erledigen morgen AI-Agenten — schneller, fehlerfreier, 24/7.

---

## Core Vision

### Problem Statement

Unternehmen mit AX 2012 R3 stehen vor einem Dilemma: Das ERP-System ist stabil und geschäftskritisch, aber seine Bedienung ist **manuell, zeitintensiv und fehleranfällig**. Jede Kundenbestellung, jede Preisanfrage, jede Reservierung erfordert menschliche Interaktion im AX-Client.

Gleichzeitig fehlt eine moderne API-Schicht: Enterprise Portal und OData sind nicht verfügbar, was die Integration mit modernen AI-Tools und Automation-Plattformen verhindert.

### Problem Impact

| Bereich | Auswirkung |
|---------|------------|
| **Vertrieb** | Langsame Auftragserfassung, Kunden warten auf Bestätigungen |
| **Customer Service** | Repetitive Anfragen binden qualifizierte Mitarbeiter |
| **SCM** | Manuelle Reservierungen führen zu Bestandsfehlern |
| **Finance** | Verzögertes Mahnwesen, manuelle Zahlungszuordnung |
| **IT** | Keine Möglichkeit, AI/LLM-Tools anzubinden |

**Opportunitätskosten:** Jede Stunde manueller AX-Arbeit ist eine Stunde, die nicht für Kundenbeziehungen, Analyse oder Wertschöpfung genutzt wird.

### Why Existing Solutions Fall Short

| Ansatz | Limitation |
|--------|------------|
| **Enterprise Portal** | Nicht verfügbar in dieser AX-Installation |
| **OData/REST APIs** | Nicht verfügbar ohne EP |
| **Direct SQL** | Security-Nightmare, keine Business Logic |
| **RPA (UI Automation)** | Fragil, langsam, keine echte Integration |
| **Custom Development** | Teuer, lange Zyklen, kein AI-Ready Interface |

**Die Lücke:** Es existiert keine sichere, AI-ready Schnittstelle zu AX 2012 R3, die sowohl lesenden als auch schreibenden Zugriff mit Enterprise-Grade Security bietet.

### Proposed Solution

**GBL-AX2012-MCP** schließt diese Lücke durch:

1. **MCP-Server als Secure Gateway**
   - Strukturierte Tools für Read/Write-Operationen
   - Role-Based Access Control (RBAC)
   - Vollständiges Audit-Logging

2. **AI-Native Integration**
   - LLM-optimierte Tool-Definitionen
   - Intent Recognition für natürliche Sprache
   - Validation Layer gegen Halluzinationen

3. **n8n Orchestration**
   - Workflow-Automation für komplexe Prozesse
   - Multi-Channel Trigger (E-Mail, Webshop, EDI, Chat, Voice)
   - Human-in-the-Loop für Exceptions

4. **Vibe-Coder Capability**
   - AI-generierte Workflows on-demand
   - Rapid Prototyping neuer Automationen
   - Self-Service für Power User

### Solution Architecture

```
Trigger (E-Mail/Webshop/EDI/Chat/Voice)
           ↓
    n8n Orchestration
           ↓
      AI/LLM Layer (Intent, Extraction, Decisions)
           ↓
    AX 2012 MCP Server (Secure Execution)
           ↓
    AX 2012 R3 (AIF/WCF/BC.NET)
```

---

## Key Differentiators & USPs

### Complete USP Matrix

| # | USP | Category | Description | Defensibility |
|---|-----|----------|-------------|---------------|
| 1 | **MCP-Native ERP Gateway** | Tech | Erster MCP-Server für Legacy-ERPs — jedes LLM-Tool kann sofort integrieren | HIGH — First mover |
| 2 | **ERP-Agnostic Abstraction** | Tech | Interface ist ERP-agnostisch. Heute AX 2012, morgen D365, übermorgen SAP | HIGH — Architecture |
| 3 | **Security-by-Design** | Tech | Tiered Approval, Idempotency Keys, Circuit Breaker, Kill Switch — Defense in Depth | MEDIUM — Copyable but complex |
| 4 | **Time-to-Value in Days** | Business | Erste Automation in Tagen, nicht Monaten — 10x schneller als klassische Integration | HIGH — Proven stack |
| 5 | **Vibe-Coder Self-Service** | Business | Power User können eigene Workflows generieren lassen ohne IT-Ticket | MEDIUM — Emerging capability |
| 6 | **Full O2C Coverage** | Business | Kompletter Order-to-Cash Cycle — nicht nur Fragments | HIGH — Scope advantage |
| 7 | **Hallucination-Proof Execution** | Tech | Validierung gegen AX-Stammdaten bevor irgendwas passiert | HIGH — Validation layer |
| 8 | **Atomic Transactions** | Tech | AX managed Transactions. Order ist komplett oder gar nicht | MEDIUM — Standard practice |
| 9 | **Conversational ERP** | UX | Natürliche Sprache statt 47 Klicks — ERP wird menschlich | HIGH — Paradigm shift |
| 10 | **Graceful Human Handoff** | UX | Smooth Handoff zu Human bei Unsicherheit oder High-Value | MEDIUM — Design pattern |
| 11 | **Multi-Channel Consistency** | UX | E-Mail, Chat, Voice, Portal — gleiche Experience überall | MEDIUM — Integration work |
| 12 | **Compliance-Ready Audit** | Enterprise | Jeder Call geloggt: User, Timestamp, Company, Payload, Result | HIGH — Built-in |
| 13 | **Chaos-Tested Resilience** | Enterprise | Jeder Failure Mode hat eine Mitigation — Production-Grade vom Tag 1 | HIGH — Proven patterns |
| 14 | **Measurable SLAs** | Enterprise | 99.5% Availability, <2% Error Rate, <500ms Read Latency | MEDIUM — Commitment |
| 15 | **MVP in 2 Weeks** | Execution | 6 P0-Tools, Health Check, Audit Log — funktionierendes Produkt | HIGH — Team capability |
| 16 | **No-Code Orchestration** | Execution | Business kann Flows anpassen ohne Deployment-Cycle | MEDIUM — n8n dependency |
| 17 | **Open Protocol + Proprietary Impl** | Strategy | MCP ist offener Standard, AX-Implementation ist proprietär | HIGH — Moat |

### Top 5 Killer USPs

| Rank | USP | Elevator Pitch |
|------|-----|----------------|
| **#1** | MCP-Native ERP Gateway | "Der erste MCP-Server für Legacy-ERPs — jedes AI-Tool kann sofort integrieren" |
| **#2** | Full O2C Automation | "Nicht nur Order Entry — kompletter Order-to-Cash in einem System" |
| **#3** | Conversational ERP | "Natürliche Sprache statt 47 Klicks — ERP wird endlich menschlich" |
| **#4** | Time-to-Value in Days | "Erste Automation in Tagen, nicht Monaten — 10x schneller als klassische Integration" |
| **#5** | Hallucination-Proof + Audit | "AI-Sicherheit für Enterprise — validiert, geloggt, compliant" |

---

## O2C Automation Scope

### Order-to-Cash Process Coverage

```
┌─────────────┐    ┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│  1. ORDER   │ →  │  2. FULFILL │ →  │  3. INVOICE │ →  │  4. PAYMENT │
│   CAPTURE   │    │   & SHIP    │    │   & DUNNING │    │   & CLOSE   │
└─────────────┘    └─────────────┘    └─────────────┘    └─────────────┘
     │                   │                   │                   │
     ▼                   ▼                   ▼                   ▼
 • Kundenanfrage     • Reservierung      • Rechnung         • Zahlungseingang
 • Preisfindung      • Kommissionierung  • Mahnwesen        • OP-Ausgleich
 • Auftragsanlage    • Lieferschein      • Kreditprüfung    • Reporting
 • Auftragsbestät.   • Versand                              
```

### Automation Use Cases by Phase

#### Phase 1: Order Capture Automation

| Trigger | AI Action | MCP Tools | Human Touchpoint |
|---------|-----------|-----------|------------------|
| E-Mail mit Bestellung | Extract: Kunde, Artikel, Menge | `ax_validate_customer`, `ax_check_stock`, `ax_simulate_price` | Bei Unklarheiten |
| Webshop Order | Direct mapping | `ax_create_salesorder` | Nur bei Kreditblock |
| Telefon (Voice) | Speech→Text→Intent | `ax_read_customer`, `ax_create_salesorder` | Bestätigung bei >€10k |
| Chat Request | Conversational | Alle Read/Write Tools | Optional |

#### Phase 2: Fulfillment Automation

| Trigger | AI Action | MCP Tools | Human Touchpoint |
|---------|-----------|-----------|------------------|
| Order confirmed | Auto-Reserve | `ax_reserve_salesline` | Bei Teillieferung |
| Stock available | Trigger Picking | `ax_update_salesorder` (Status) | Warehouse bestätigt |
| Shipped | Update Tracking | `ax_post_shipment` | — |

#### Phase 3: Invoice & Dunning Automation

| Trigger | AI Action | MCP Tools | Human Touchpoint |
|---------|-----------|-----------|------------------|
| Shipment posted | Generate Invoice | `ax_create_invoice` | — |
| Payment overdue | Dunning Level Check | `ax_read_customer_aging` | Level 3+ |
| Dispute received | Classify & Route | `ax_read_invoice`, `ax_add_note` | Always |

#### Phase 4: Payment & Close

| Trigger | AI Action | MCP Tools | Human Touchpoint |
|---------|-----------|-----------|------------------|
| Bank Statement | Match & Post | `ax_post_payment`, `ax_settle_invoice` | Unmatched items |
| All settled | Close Order | `ax_close_salesorder` | — |

---

## Technology Stack

### Integration Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                        TRIGGER LAYER                                 │
│  E-Mail │ Webshop │ EDI │ Chat │ Telefon (Voice→Text) │ Portal      │
└────────────────────────────┬────────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────────┐
│                     n8n ORCHESTRATION                                │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐ │
│  │ Order Flow  │  │ Fulfill Flow│  │Invoice Flow │  │Payment Flow │ │
│  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘ │
└─────────┼────────────────┼────────────────┼────────────────┼────────┘
          │                │                │                │
          ▼                ▼                ▼                ▼
┌─────────────────────────────────────────────────────────────────────┐
│                      AI/LLM LAYER                                    │
│  • Intent Recognition (Was will der Kunde?)                         │
│  • Entity Extraction (Kunde, Artikel, Menge, Preis)                 │
│  • Decision Making (Kreditlimit OK? Bestand da?)                    │
│  • Exception Handling (Unklare Fälle → Human)                       │
│  • Vibe-Coder: Generiert neue Flows on-the-fly                      │
└────────────────────────────┬────────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────────┐
│                    AX 2012 MCP SERVER                                │
│  ┌──────────────────────────────────────────────────────────────┐   │
│  │ READ TOOLS          │ WRITE TOOLS         │ VALIDATION       │   │
│  │ ax_read_customer    │ ax_create_salesorder│ ax_validate_*    │   │
│  │ ax_read_inventory   │ ax_add_salesline    │ ax_check_credit  │   │
│  │ ax_read_salesorder  │ ax_reserve_line     │ ax_check_stock   │   │
│  │ ax_read_invoice     │ ax_create_invoice   │                  │   │
│  │ ax_read_payment     │ ax_post_payment     │                  │   │
│  └──────────────────────────────────────────────────────────────┘   │
└────────────────────────────┬────────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────────┐
│                    AX 2012 R3 (AIF/WCF/BC.NET)                       │
└─────────────────────────────────────────────────────────────────────┘
```

### MCP Server Resilience Patterns

```
┌─────────────────────────────────────────────────────────────────────┐
│                    MCP SERVER RESILIENCE                             │
├─────────────────────────────────────────────────────────────────────┤
│  Rate Limiter → Input Validator → Circuit Breaker                   │
│  (100/min/user)  (Schema+AX-Ref)   (30s timeout, 3 fail)            │
│         ↓              ↓                  ↓                          │
│  Audit Logger    Idempotency Key    Retry Handler                   │
│  (DB + File)        Store          (Exp. Backoff, max 2)            │
│         ↓              ↓                  ↓                          │
│  ┌─────────────────────────────────────────────────────────────────┐│
│  │         HEALTH MONITOR + ALERTING                               ││
│  │  - AOS connectivity (30s interval)                              ││
│  │  - Memory/CPU thresholds                                        ││
│  │  - Error rate spike detection                                   ││
│  └─────────────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────────┘
```

---

## Target Users

### Primary Users

#### 👔 Persona 1: Stefan — Vertriebsmitarbeiter

**Kontext:**
- 38 Jahre, 8 Jahre im Unternehmen
- Bearbeitet 30-50 Aufträge pro Tag
- Arbeitet hauptsächlich im AX-Client und Outlook

**Problem Experience:**
- Verbringt 60% seiner Zeit mit Dateneingabe statt Kundenbetreuung
- Copy-Paste zwischen E-Mail und AX ist fehleranfällig
- Kunden warten auf Auftragsbestätigungen während er tippt
- Preisanfragen erfordern 5+ Klicks im AX-Client
- **Deep Pain:** Sonderpreise/Kundenkonditionen werden bei manueller Eingabe vergessen

**Ziele:**
- Schnellere Auftragserfassung
- Mehr Zeit für Kundenbeziehungen
- Weniger Tippfehler bei Bestellungen

**Interaktion mit MCP:**
- Primär über Chat/Conversational Interface
- "Leg Auftrag für Müller an, 50 Stück Widget Pro, Standardpreis"
- Bekommt sofortige Bestätigung mit SalesId

**Success Moment:**
> "Ich hab den Auftrag in 10 Sekunden statt 5 Minuten erfasst — und der Kunde hat die Bestätigung schon!"

---

#### 📞 Persona 2: Lisa — Customer Service Mitarbeiterin

**Kontext:**
- 29 Jahre, 3 Jahre im Unternehmen
- Bearbeitet Nachbestellungen, Retouren, Statusanfragen
- Telefon + E-Mail als Hauptkanäle

**Problem Experience:**
- Muss während Telefonat im AX suchen — Kunde wartet
- Häufige Fragen: "Wo ist meine Bestellung?" "Kann ich noch was hinzufügen?"
- Retouren-Prozess ist komplex und fehleranfällig
- **Deep Pain:** Keine Prognose-Fähigkeit ("Wann ist Artikel wieder da?")

**Ziele:**
- Sofortige Antworten während Kundengespräch
- Einfache Auftragsänderungen
- Weniger Rückrufe wegen fehlender Infos

**Interaktion mit MCP:**
- Voice-to-Text während Telefonat (mit Confidence Score + Live-Preview)
- "Zeig mir alle offenen Aufträge für Kunde Schmidt"
- "Füge 10 Stück Artikel ABC zur Bestellung 12345 hinzu"

**Success Moment:**
> "Der Kunde hat aufgelegt und alles war erledigt — kein Rückruf nötig!"

---

#### 📦 Persona 3: Thomas — SCM / Lager-Disponent

**Kontext:**
- 45 Jahre, 15 Jahre im Unternehmen
- Verantwortlich für Reservierungen und Bestandsmanagement
- Arbeitet mit Lagerteam und Vertrieb

**Problem Experience:**
- Reservierungskonflikte zwischen Aufträgen
- Manuelle Lagerumbuchungen sind zeitaufwändig
- Bestandsabfragen erfordern mehrere Reports
- **Deep Pain:** Priorisierungs-Chaos bei Teillieferungen — wer bekommt was zuerst?

**Ziele:**
- Automatische Reservierung bei Auftragseingang
- Echtzeit-Bestandsübersicht
- Weniger manuelle Korrekturen
- Klare Priorisierungsregeln

**Interaktion mit MCP:**
- Automatisierte Flows via n8n
- Alerts bei Bestandsengpässen
- "Reserviere alle offenen Positionen für Auftrag 12345"
- Priority-Parameter für Reservierungen

**Success Moment:**
> "Die Reservierungen laufen automatisch — ich kümmere mich nur noch um Ausnahmen!"

---

#### 💰 Persona 4: Claudia — Debitorenbuchhalterin

**Kontext:**
- 52 Jahre, 20 Jahre im Unternehmen
- Verantwortlich für Mahnwesen, Kundenanlage, Kreditlimits
- Arbeitet eng mit Vertrieb zusammen

**Problem Experience:**
- Kundenanlage ist ein 15-Minuten-Prozess
- Mahnläufe erfordern manuelle Vorbereitung
- Kreditlimit-Prüfungen verzögern Aufträge
- **Deep Pain:** Externe Validierungen (USt-ID, Kreditauskunft) sind nicht integriert

**Ziele:**
- Schnellere Kundenanlage (Multi-Step mit Validierungen)
- Automatisierte Mahnvorbereitung
- Proaktive Kreditwarnungen

**Interaktion mit MCP:**
- Approval-Workflow für neue Kunden
- "Leg Kunde Müller GmbH an mit Standardkonditionen"
- Automatische Alerts bei Kreditüberschreitung
- Finance Approval Queue für Großaufträge

**Success Moment:**
> "Neuer Kunde ist in 2 Minuten angelegt — früher war das ein halber Tag!"

---

### Secondary Users

#### 🔧 Persona 5: Markus — IT-Administrator / AX-Admin

**Kontext:**
- 35 Jahre, 5 Jahre im Unternehmen
- Verantwortlich für AX-Betrieb und Integrationen
- Einziger mit tiefem AX-Wissen

**Problem Experience:**
- Jede Integration ist ein Projekt
- Keine Standard-API für externe Systeme
- Troubleshooting ohne Logs ist Blindflug
- **Deep Pain:** Keine Zeit für Log-Analyse — braucht Dashboards, nicht Rohdaten

**Ziele:**
- Standardisierte Integration für alle Systeme
- Vollständige Audit-Logs mit Dashboard + Anomalie-Alerts
- Health Monitoring ohne AX-Client

**Interaktion mit MCP:**
- Admin-Dashboard für Health/Logs/Anomalien
- `ax_health_check` für Monitoring
- Konfiguration von Rollen und Berechtigungen

**Success Moment:**
> "Neue Integration? Ich geb denen MCP-Zugang und fertig — kein Custom Code!"

**Key Insight:** MCP ist IT-Entlastung. Einmal bauen, alle profitieren.

---

#### 🤖 Persona 6: AI-Agent (Autonomous)

**Kontext:**
- Kein Mensch — autonomer Prozess
- Verarbeitet E-Mails, Webshop-Orders, EDI-Nachrichten
- Läuft 24/7 ohne menschliche Interaktion

**Problem Experience:**
- Kann nicht mit AX kommunizieren
- Jede Aktion erfordert menschliche Vermittlung
- Keine Möglichkeit zur Selbstkorrektur
- **Deep Pain:** Unklare Escalation-Mechanik — WIE wird ein Human benachrichtigt?

**Ziele:**
- Direkte AX-Operationen ohne Human-in-Loop
- Validierung vor Ausführung (Fuzzy Match Confirmation)
- Graceful Escalation mit definierten Kanälen (Teams, E-Mail, Ticket) + SLA

**Interaktion mit MCP:**
- Vollautomatische Tool-Chains
- E-Mail → Parse → Validate → Create Order → Confirm
- Escalation an Human nur bei Exceptions

**Success Moment:**
> "100 Orders über Nacht verarbeitet — 0 Fehler, 0 menschliche Eingriffe!"

---

### User Journey

#### Discovery → Onboarding → Core Usage → Success

```
┌─────────────┐    ┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│  DISCOVERY  │ →  │  ONBOARDING │ →  │ CORE USAGE  │ →  │   SUCCESS   │
└─────────────┘    └─────────────┘    └─────────────┘    └─────────────┘
      │                  │                  │                  │
      ▼                  ▼                  ▼                  ▼
 IT zeigt Demo      Rolle zuweisen     Daily Operations   Messbare KPIs
 "Das geht?"        Erste Commands     Conversational     Zeit gespart
 Pilot-User         Cheat Sheet        Automation         Fehler reduziert
```

**Stefan's Journey (Vertrieb):**
1. **Discovery:** IT zeigt Demo — "Auftrag per Chat? Das geht?"
2. **Onboarding:** Bekommt MCP_Sales_Write Rolle, 10-Minuten-Intro, Cheat Sheet
3. **First Win:** Erster Auftrag in 15 Sekunden statt 5 Minuten
4. **Core Usage:** 80% der Aufträge über Chat, nur Sonderfälle im AX-Client
5. **Success:** 2 Stunden/Tag gespart, Kundenzufriedenheit gestiegen

**AI-Agent Journey:**
1. **Discovery:** IT evaluiert MCP für E-Mail-Automation
2. **Onboarding:** n8n Flow konfiguriert, Test mit 10 E-Mails in Sandbox
3. **First Win:** Erste automatische Order ohne Fehler
4. **Core Usage:** 24/7 Verarbeitung aller Kanäle
5. **Success:** 95% Automation Rate, <2% Escalation

---

### Cross-Functional Scenario: Großauftrag mit Kreditrisiko

**Situation:** Neukunde bestellt für €150.000. Kreditlimit ist €100.000.

**Lösung: Conditional Order Release**

```
IF Kreditlimit überschritten:
  1. Auftrag splitten in "unter Limit" + "über Limit"
  2. Teil 1: Sofort freigeben → SCM kann kommissionieren
  3. Teil 2: Warten auf Anzahlung/Freigabe Finance
  4. SCM bekommt klare Freigabe-Signale
```

| Rolle | Aktion | MCP Tool |
|-------|--------|----------|
| Stefan (Vertrieb) | Auftrag anlegen | `ax_create_salesorder` |
| System | Kreditprüfung | `ax_check_credit` |
| System | Auto-Split | `ax_split_order_by_credit` |
| Claudia (Finance) | Approval Queue | Dashboard |
| Thomas (SCM) | Freigabe-Signal | `ax_release_for_picking` |

---

## Additional Tools Identified

### From User Research

| Tool | Description | Priority |
|------|-------------|----------|
| `ax_check_availability_forecast` | Wann ist Artikel wieder verfügbar? | P1 |
| `ax_update_delivery_date` | Liefertermin ändern | P1 |
| `ax_send_order_confirmation` | Bestätigung per E-Mail senden | P2 |
| `ax_get_reservation_queue` | Wer wartet noch auf diesen Artikel? | P2 |
| `ax_split_order_by_credit` | Auftrag bei Kreditüberschreitung splitten | P2 |
| `ax_release_for_picking` | Freigabe-Signal an Lager | P2 |

---

## Risk Mitigations (Enhanced from Pre-mortem)

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Security Incident (Test in Prod) | MEDIUM | CRITICAL | Sandbox/Prod Trennung technisch enforced |
| AX Performance Degradation | HIGH | HIGH | Load Testing vor Go-Live, dedizierter AOS |
| User Rejection | HIGH | HIGH | Pilot mit Power Usern, Cheat Sheet, Feedback-Loop |
| Data Quality (Kundenverwechslung) | MEDIUM | HIGH | Fuzzy Match Confirmation |
| Scope Creep | HIGH | MEDIUM | Strict MVP (6 Tools), Phased Rollout |

---

## Additional USP

### USP #18: IT-Entlastung

**Root Cause Analysis (5 Whys):**
```
Warum verbringt Stefan 60% mit Dateneingabe?
→ Weil jede Bestellung manuell erfasst werden muss
→ Weil keine automatische Schnittstelle existiert
→ Weil AX 2012 keine moderne API hat
→ Weil Custom Development teuer ist und IT-Ressourcen knapp
→ Weil IT mit Maintenance beschäftigt ist
```

**Insight:** MCP ist nicht nur ein Tool für User — es ist eine **IT-Entlastung**.

**Elevator Pitch:**
> "Einmal bauen, alle profitieren. MCP ist die API-Schicht die IT seit Jahren braucht — ohne Custom Development für jeden Use Case."

---

## Success Metrics

### User Success Metrics

| Persona | Success Metric | Target | Measurement |
|---------|---------------|--------|-------------|
| **Stefan (Vertrieb)** | Zeit pro Auftragserfassung | <30 Sekunden (vs. 5 Min heute) | Timer in MCP |
| **Stefan** | Aufträge über MCP vs. AX-Client | >80% über MCP | Usage Analytics |
| **Lisa (Customer Service)** | First-Call-Resolution Rate | >90% | CRM Tracking |
| **Lisa** | Rückruf-Quote | <10% (vs. 40% heute) | Call Logs |
| **Thomas (SCM)** | Manuelle Reservierungs-Eingriffe | <5% aller Orders | Exception Log |
| **Thomas** | Reservierungs-Konflikte | -80% vs. Baseline | AX Reports |
| **Claudia (Finance)** | Zeit für Kundenanlage | <5 Min (vs. 15 Min heute) | Process Timer |
| **Claudia** | Kreditüberschreitungs-Incidents | -50% | Finance Reports |
| **Markus (IT)** | Integration-Requests an IT | -70% | Ticket System |
| **Markus** | Mean Time to Detect Issues | <5 Min | Monitoring |
| **AI-Agent** | Automation Rate | >95% ohne Human | Audit Log |
| **AI-Agent** | Escalation Rate | <5% | Escalation Queue |

### User Success Moments

| Persona | "Aha!" Moment |
|---------|---------------|
| Stefan | "Ich hab den Auftrag in 10 Sekunden erfasst — und der Kunde hat die Bestätigung schon!" |
| Lisa | "Der Kunde hat aufgelegt und alles war erledigt — kein Rückruf nötig!" |
| Thomas | "Die Reservierungen laufen automatisch — ich kümmere mich nur noch um Ausnahmen!" |
| Claudia | "Neuer Kunde ist in 2 Minuten angelegt — früher war das ein halber Tag!" |
| Markus | "Neue Integration? Ich geb denen MCP-Zugang und fertig — kein Custom Code!" |
| AI-Agent | "100 Orders über Nacht verarbeitet — 0 Fehler, 0 menschliche Eingriffe!" |

---

### Business Objectives

#### 3-Monats-Ziele (MVP Launch)

| Objective | Target | Measurement |
|-----------|--------|-------------|
| MCP Server live in Production | ✅ Deployed | Deployment Status |
| 6 P0-Tools funktional | 100% | Test Suite |
| Pilot-User aktiv | 5 Power User | Usage Analytics |
| Zero Security Incidents | 0 | Incident Log |
| AX Performance unbeeinträchtigt | <5% Degradation | AOS Monitoring |

#### 6-Monats-Ziele (Rollout)

| Objective | Target | Measurement |
|-----------|--------|-------------|
| User Adoption Vertrieb | >50% der Vertriebler | Usage Analytics |
| Order Capture Automation | >30% aller Orders | Audit Log |
| Time Savings Vertrieb | 2h/Tag/Person | Process Comparison |
| Error Rate Reduction | -50% vs. Baseline | Quality Reports |
| IT Integration Requests | -50% | Ticket System |

#### 12-Monats-Ziele (Full O2C)

| Objective | Target | Measurement |
|-----------|--------|-------------|
| Full O2C Coverage | Alle 4 Phasen live | Feature Tracking |
| End-to-End Automation Rate | >60% | Audit Log |
| Cost Savings | €X/Jahr (TBD) | Finance Analysis |
| Customer Satisfaction | +10 NPS | Survey |
| D365 Migration Ready | Interface abstrahiert | Architecture Review |

---

### Key Performance Indicators (KPIs)

#### Technical KPIs

| KPI | Target | Measurement | Alert Threshold |
|-----|--------|-------------|-----------------|
| **Availability** | 99.5% | Uptime Monitoring | <99% |
| **Read Latency (p95)** | <500ms | APM | >1s |
| **Write Latency (p95)** | <2s | APM | >5s |
| **Error Rate** | <2% | Audit Log | >5% |
| **Circuit Breaker Trips** | <1/day | Health Monitor | >3/day |

#### Operational KPIs

| KPI | Target | Measurement | Alert Threshold |
|-----|--------|-------------|-----------------|
| **Daily Active Users** | >20 | Usage Analytics | <10 |
| **Orders via MCP** | >100/day | Audit Log | <50 |
| **Escalation Rate** | <5% | Escalation Queue | >10% |
| **Human Approval Time** | <30 min | Workflow Timer | >2h |
| **Audit Log Completeness** | 100% | Log Validation | <100% |

#### Business KPIs

| KPI | Target | Measurement | Alert Threshold |
|-----|--------|-------------|-----------------|
| **Time Saved (Vertrieb)** | 2h/day/person | Process Timer | <1h |
| **Order Error Rate** | <1% | Quality Reports | >3% |
| **Customer Response Time** | <2 min | CRM | >5 min |
| **IT Ticket Reduction** | -70% | Ticket System | <-30% |

---

### Success Metrics Dashboard

```
┌─────────────────────────────────────────────────────────────────────┐
│                    GBL-AX2012-MCP DASHBOARD                          │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  HEALTH          USAGE           PERFORMANCE      BUSINESS           │
│  ┌─────────┐    ┌─────────┐    ┌─────────┐      ┌─────────┐         │
│  │ 99.7%   │    │ 127     │    │ 320ms   │      │ 2.1h    │         │
│  │ Uptime  │    │ Orders  │    │ Latency │      │ Saved   │         │
│  │ ✅      │    │ Today   │    │ ✅      │      │ /Person │         │
│  └─────────┘    └─────────┘    └─────────┘      └─────────┘         │
│                                                                      │
│  AUTOMATION      ERRORS          ESCALATIONS     ADOPTION            │
│  ┌─────────┐    ┌─────────┐    ┌─────────┐      ┌─────────┐         │
│  │ 94.2%   │    │ 1.3%    │    │ 3.8%    │      │ 67%     │         │
│  │ Auto    │    │ Rate    │    │ Rate    │      │ Users   │         │
│  │ ✅      │    │ ✅      │    │ ✅      │      │ Active  │         │
│  └─────────┘    └─────────┘    └─────────┘      └─────────┘         │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘
```

---

### Metrics-to-Strategy Alignment

| Product Vision | User Metric | Business Metric | KPI |
|----------------|-------------|-----------------|-----|
| "Manuelle Prozesse automatisieren" | Automation Rate >95% | Time Saved 2h/day | Orders via MCP |
| "AI-gestützte Automatisierung" | Escalation Rate <5% | Error Rate -50% | Escalation Queue |
| "Sichere Bereitstellung" | Zero Security Incidents | Audit Completeness 100% | Availability 99.5% |
| "Full O2C Coverage" | All Personas active | All 4 Phases live | Feature Coverage |
| "IT-Entlastung" | IT Tickets -70% | Integration Requests -70% | Ticket Reduction |

---

### Anti-Metrics (What NOT to Optimize)

| Anti-Metric | Why Not | Better Alternative |
|-------------|---------|-------------------|
| "Number of Tools" | More tools ≠ more value | Tool Usage Rate |
| "Lines of Code" | Complexity ≠ quality | Test Coverage |
| "Features Shipped" | Features ≠ outcomes | User Success Rate |
| "Uptime 100%" | Unrealistic, drives wrong behavior | 99.5% with fast recovery |
