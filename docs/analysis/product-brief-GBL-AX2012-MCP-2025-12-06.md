---
stepsCompleted: [1, 2]
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
