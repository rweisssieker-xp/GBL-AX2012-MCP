---
stepsCompleted: [1, 2, 3]
inputDocuments: []
session_topic: "AX 2012 R3 MCP Server - Read/Write Integration Layer"
session_goals: "Gap analysis, risk identification, architecture refinement, implementation prioritization"
selected_approach: "Progressive Technique Flow"
techniques_used: ["What If Scenarios", "Morphological Analysis", "Chaos Engineering"]
ideas_generated: ["Tiered Approval Model", "Circuit Breaker Pattern", "Idempotency Keys", "Pre-Validation Tools", "Kill Switch", "Revised MVP Scope"]
context_file: ".bmad/bmm/data/project-context-template.md"
status: "COMPLETED"
---

# Brainstorming Session: AX 2012 R3 MCP Server

**Date:** 2025-12-06  
**Facilitator:** Mary (Business Analyst)  
**Participant:** Reinerw

## Session Overview

**Topic:** Building an MCP (Model Context Protocol) Server for Microsoft Dynamics AX 2012 R3 CU13 that enables structured, secure, and auditable read/write operations via LLM-based clients.

**Goals:**
- Read access to core AX data (Customers, Items, Inventory, Sales, Finance)
- Write access for Sales Order creation/updates, Customer creation, Reservations, Price simulation
- Automation of manual AX client operations
- Secure tool catalog for LLM applications
- Role-based access control with comprehensive audit logging

**Architecture:**
- AIF Services (Read/Write)
- Custom WCF Services (Write, transactional)
- Business Connector .NET (local automation, admin)

**Key Constraints:**
- No Enterprise Portal / OData available
- BC.NET must run locally (AX-Client or Server with BC components)
- Atomic transactions managed by AX

### Session Setup

**Selected Approach:** Progressive Technique Flow  
**Technique Sequence:**
1. **What If Scenarios** (Creative) — Start broad, explore radical possibilities
2. **Morphological Analysis** (Deep) — Systematically narrow to concrete combinations
3. **Chaos Engineering** (Wild) — Stress-test with failure scenarios

---

## Phase 1: What If Scenarios — Insights

### Architecture & Integration

| Question | Insight | Erkenntnis |
|----------|---------|------------|
| Was wenn AIF nicht existiert? | Direct SQL = Security-Nightmare. Alternative: X++ Batch + File Exchange | AIF ist trotz Komplexität sicherster Weg; Custom Services für Lücken |
| Was wenn BC.NET remote laufen könnte? | Wäre First Choice — schneller, direkter | BC.NET für lokale Admin-Tools maximieren; AIF/WCF für Remote |
| Unlimited Budget für Middleware? | Azure Service Bus + API Management | Auch ohne Budget: Circuit Breaker, Retry, Request Queue selbst implementieren |

### Security & Governance

| Question | Insight | Erkenntnis |
|----------|---------|------------|
| Jede Write-Op braucht Approval? | Finance-Writes: Ja. Sales Order: Zu langsam | Tiered Approval: Low-Risk=Auto, Medium=Queue, High=Explicit |
| MCP-Server gehackt? | Blast Radius: Alle Writes des kompromittierten Users | Rate Limiting, Anomaly Detection, Kill Switch, Separate Service Accounts |
| LLM-Client nicht vertrauenswürdig? | Halluziniert IDs, erfindet Artikel | ALLE Inputs validieren, keine "Create if not exists", strikte Schemas |

### Users & Operations

| Question | Insight | Erkenntnis |
|----------|---------|------------|
| User definieren eigene Tools? | Mächtig aber Governance-Albtraum | NEIN für v1.0. Später evtl. Macro-Tools |
| System muss offline arbeiten? | Reads: Caching OK. Writes: Gefährlich | Read-Cache (TTL 15min), Write-Queue nur idempotent, Sales Order MUSS sync sein |
| 10x User-Load? | Bottlenecks: AIF Throughput, AOS Threads, DB Connections | Connection Pooling, Bulk-Operations, Read-Replika |

### Timeline & Scope

| Question | Insight | Erkenntnis |
|----------|---------|------------|
| Release 1.0 in 2 Wochen? | MVP: 3-4 Tools, File-based Audit, eine Rolle | Roadmap 1.0 zu ambitioniert |
| Skip zu D365? | Projekt ist BRIDGE, nicht Endlösung | Interface abstrakt halten, Adapter-Pattern für D365 |
| Projekt scheitert komplett? | Fallback: Manuelle AX-Arbeit | Success Metrics VOR Go-Live definieren! |

---

## Phase 2: Morphological Analysis — Kombinations-Matrix

### Parameter-Dimensionen

| Dimension | Option A | Option B | Option C |
|-----------|----------|----------|----------|
| **Interface** | AIF Standard | Custom WCF | BC.NET |
| **Auth Model** | Windows Auth | Service Account | Token-Based |
| **Transaction** | Sync (blocking) | Async (queue) | Batch (scheduled) |
| **Validation** | Client-side | Server-side | AX-side |
| **Error Handling** | Fail-fast | Retry+Circuit | Queue+Retry |
| **Audit** | File Log | Database | Event Stream |

### Optimale Kombinationen pro Use Case

| Use Case | Interface | Auth | Transaction | Validation | Errors | Audit |
|----------|-----------|------|-------------|------------|--------|-------|
| **Sales Order Create** | Custom WCF | Service Account | Sync | Server+AX | Retry+Circuit | Database |
| **Read Customer** | AIF Standard | Windows Auth | Sync | Server | Fail-fast | File Log |
| **Read Inventory** | AIF Standard | Windows Auth | Sync | Server | Fail-fast | File Log |
| **Reserve Sales Line** | Custom WCF | Service Account | Sync | Server+AX | Retry+Circuit | Database |
| **Create Customer** | Custom WCF | Service Account | Sync | Server+AX | Fail-fast | Database |
| **Price Simulation** | Custom WCF | Service Account | Sync | Server | Retry+Circuit | File Log |
| **Inventory Movement** | Custom WCF | Service Account | Async | Server+AX | Queue+Retry | Event Stream |
| **Health Check** | BC.NET | Windows Auth | Sync | - | Fail-fast | File Log |

### Key Decisions

1. **Alle Writes → Custom WCF + Service Account + Database Audit**
2. **Alle Reads → AIF Standard + Windows Auth + File Log**
3. **Inventory Movements → Einziger Async Use Case (Queue)**
4. **BC.NET → Nur für Admin/Health, nicht für Business Operations**

---

## Phase 3: Chaos Engineering — Failure Scenarios

### Failure Scenario Matrix

| # | Failure | Impact | Likelihood | Mitigation |
|---|---------|--------|------------|------------|
| 1 | AIF Service Timeout | Sales Order hängt | HIGH | Circuit Breaker (30s), Retry max 2x |
| 2 | AOS Crash während Write | Transaktion unklar | MEDIUM | Idempotency Keys, Status-Check-Tool |
| 3 | Inventory Reservation Conflict | Zwei User, gleicher Bestand | HIGH | Optimistic Locking (AX), Error Mapping |
| 4 | Invalid Customer ID vom LLM | Write schlägt fehl | HIGH | Pre-Validation Tool |
| 5 | Service Account PW expired | Alle Writes down | MEDIUM | Monitoring + Alert |
| 6 | MCP Server Memory Leak | Degradation → Crash | LOW | Health Endpoint, Auto-Restart |
| 7 | DoS durch Massenanfragen | AX überlastet | MEDIUM | Rate Limiting (100/min/user) |
| 8 | Audit Log voll | Writes blockiert | LOW | Log Rotation, Disk Monitoring |
| 9 | SQL Injection Payload | Potentiell gefährlich | LOW | AIF Contract + Input Sanitization |
| 10 | Netzwerk-Partition MCP↔AX | Alle Ops timeout | LOW | Graceful Degradation, Cached Reads |

### Required Resilience Patterns

```
┌─────────────────────────────────────────────────────────────┐
│                    MCP SERVER RESILIENCE                     │
├─────────────────────────────────────────────────────────────┤
│  Rate Limiter → Input Validator → Circuit Breaker           │
│  (100/min/user)  (Schema+AX-Ref)   (30s timeout, 3 fail)    │
│         ↓              ↓                  ↓                  │
│  Audit Logger    Idempotency Key    Retry Handler           │
│  (DB + File)        Store          (Exp. Backoff, max 2)    │
│         ↓              ↓                  ↓                  │
│  ┌─────────────────────────────────────────────────────────┐│
│  │         HEALTH MONITOR + ALERTING                       ││
│  │  - AOS connectivity (30s interval)                      ││
│  │  - Memory/CPU thresholds                                ││
│  │  - Error rate spike detection                           ││
│  └─────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────┘
```

### Chaos Test Plan

| Test | Method | Expected Result |
|------|--------|-----------------|
| Kill AOS mid-request | Stop AOS during Sales Order Create | Timeout error, no partial data |
| Flood requests | 500 requests in 10s | Rate limiter, 429 responses |
| Invalid payloads | Malformed JSON, wrong types | 400 Bad Request, no AX call |
| Duplicate submission | Same idempotency key twice | Cached result returned |
| Network latency | 5s delay to AX | Circuit breaker opens |

---

## Consolidated Decisions & Action Items

### Architecture Decisions (FINAL)

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Primary Write Interface | Custom WCF Services | AIF zu limitiert |
| Primary Read Interface | AIF Standard Services | Bereits vorhanden, stabil |
| Auth Strategy | Service Accounts per Role | Granulare Kontrolle |
| Transaction Model | Synchron für alle Writes | Consistency > Performance |
| Audit Storage | Database + File Backup | Queryable + Resilient |
| Error Strategy | Circuit Breaker + Retry (max 2) | Balance Resilience/UX |

### Risk Mitigations (MUST HAVE v1.0)

- [x] Rate Limiting (100 req/min/user)
- [x] Input Validation gegen AX-Stammdaten
- [x] Circuit Breaker (30s timeout)
- [x] Idempotency Keys für Writes
- [x] Database Audit Log
- [x] Health Check Endpoint
- [x] Kill Switch für Write-Operations

### Revised MVP Scope

| Tool | Priority | Complexity |
|------|----------|------------|
| `ax_read_customer` | P0 | Low |
| `ax_read_inventory` | P0 | Low |
| `ax_read_salesorder` | P0 | Low |
| `ax_create_salesorder` | P0 | High |
| `ax_validate_customer` | P0 | Low |
| `ax_health_check` | P0 | Low |
| `ax_add_salesline` | P1 | Medium |
| `ax_create_customer` | P1 | Medium |

### Success Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| Availability | 99.5% | Uptime monitoring |
| Error Rate | < 2% | Audit log analysis |
| Latency (Read) | < 500ms p95 | APM |
| Latency (Write) | < 2s p95 | APM |
| User Adoption | > 50% target users | Usage analytics |
| Security Incidents | 0 | Incident tracking |

---

## Next Steps

1. **Validate Architecture** with AX-Admin (AIF/WCF availability)
2. **Create Technical Spec** from these decisions
3. **Prototype** `ax_health_check` + `ax_read_customer` first
4. **Define Service Contracts** for Custom WCF Services
5. **Set up Audit Database** schema

