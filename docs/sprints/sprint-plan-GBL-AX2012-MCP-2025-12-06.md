---
stepsCompleted: [1, 2, 3, 4, 5, 6]
inputDocuments:
  - "docs/epics/epics-GBL-AX2012-MCP-2025-12-06.md"
workflowType: "sprint-planning"
status: "COMPLETED"
project_name: "GBL-AX2012-MCP"
user_name: "Reinerw"
date: "2025-12-06"
---

# Sprint Plan: GBL-AX2012-MCP

**Version:** 1.0  
**Date:** 2025-12-06  
**Author:** Reinerw  
**Sprint Duration:** 2 weeks

---

## Sprint Overview

| Sprint | Weeks | Epic | Stories | Goal |
|--------|-------|------|---------|------|
| **Sprint 1** | 1-2 | Epic 1 | 1.1-1.4 | Project Setup & Core Infrastructure |
| **Sprint 2** | 3-4 | Epic 1 | 1.5-1.8 | Resilience Patterns & Basic Health |
| **Sprint 3** | 5-6 | Epic 2 | 2.1-2.3 | AIF Client & Customer Read |
| **Sprint 4** | 7-8 | Epic 2 + 3 | 2.4-2.6, 3.1-3.2 | Sales Order Read & Inventory |
| **Sprint 5** | 9-10 | Epic 3 + 4 | 3.3-3.4, 4.1-4.2 | Pricing & WCF Setup |
| **Sprint 6** | 11-12 | Epic 4 | 4.3-4.7 | Order Creation Complete |
| **Sprint 7** | 13-14 | Epic 5 | 5.1-5.5 | Security & Audit |
| **Sprint 8** | 15-16 | Epic 6 | 6.1-6.4 | Monitoring & Production Ready |

---

## Sprint 1: Project Setup & Core Infrastructure

**Duration:** Week 1-2  
**Epic:** 1 (Foundation & Infrastructure)  
**Goal:** Establish project structure, configuration, and DI foundation

### Stories

| Story | Title | Priority | Complexity |
|-------|-------|----------|------------|
| 1.1 | Project Setup & Solution Structure | P0 | Low |
| 1.2 | Configuration System | P0 | Low |
| 1.3 | Dependency Injection Setup | P0 | Medium |
| 1.4 | MCP Server Host | P0 | Medium |

### Sprint Backlog

```
┌─────────────────────────────────────────────────────────────────────┐
│ SPRINT 1 BOARD                                                       │
├─────────────────┬─────────────────┬─────────────────┬───────────────┤
│ TODO            │ IN PROGRESS     │ REVIEW          │ DONE          │
├─────────────────┼─────────────────┼─────────────────┼───────────────┤
│ □ 1.1 Project   │                 │                 │               │
│ □ 1.2 Config    │                 │                 │               │
│ □ 1.3 DI Setup  │                 │                 │               │
│ □ 1.4 MCP Host  │                 │                 │               │
└─────────────────┴─────────────────┴─────────────────┴───────────────┘
```

### Definition of Done

- [ ] All 4 stories completed
- [ ] Solution builds without errors
- [ ] `dotnet test` passes
- [ ] MCP Server starts and responds to initialization
- [ ] Configuration loads from appsettings.json
- [ ] Code reviewed and merged to main

### Risks & Mitigations

| Risk | Mitigation |
|------|------------|
| .NET 8 compatibility issues | Use LTS packages only |
| MCP SDK learning curve | Spike in first 2 days |

---

## Sprint 2: Resilience Patterns & Basic Health

**Duration:** Week 3-4  
**Epic:** 1 (Foundation & Infrastructure)  
**Goal:** Implement resilience patterns and basic health check

### Stories

| Story | Title | Priority | Complexity |
|-------|-------|----------|------------|
| 1.5 | Rate Limiter Implementation | P0 | Medium |
| 1.6 | Circuit Breaker Implementation | P0 | Medium |
| 1.7 | Tool Base Class | P0 | Medium |
| 1.8 | Health Check Tool (Basic) | P0 | Low |

### Sprint Backlog

```
┌─────────────────────────────────────────────────────────────────────┐
│ SPRINT 2 BOARD                                                       │
├─────────────────┬─────────────────┬─────────────────┬───────────────┤
│ TODO            │ IN PROGRESS     │ REVIEW          │ DONE          │
├─────────────────┼─────────────────┼─────────────────┼───────────────┤
│ □ 1.5 Rate Lim  │                 │                 │               │
│ □ 1.6 Circuit   │                 │                 │               │
│ □ 1.7 ToolBase  │                 │                 │               │
│ □ 1.8 Health    │                 │                 │               │
└─────────────────┴─────────────────┴─────────────────┴───────────────┘
```

### Definition of Done

- [ ] Rate limiter blocks requests over 100/min/user
- [ ] Circuit breaker opens after 3 failures
- [ ] ToolBase provides consistent validation/audit
- [ ] `ax_health_check` returns server status
- [ ] Unit tests for all resilience patterns

---

## Sprint 3: AIF Client & Customer Read

**Duration:** Week 5-6  
**Epic:** 2 (Read Operations)  
**Goal:** Connect to AX via AIF and implement customer lookup

### Stories

| Story | Title | Priority | Complexity |
|-------|-------|----------|------------|
| 2.1 | AIF Client Setup | P0 | High |
| 2.2 | Get Customer Tool - By Account | P0 | Medium |
| 2.3 | Get Customer Tool - Fuzzy Search | P0 | Medium |

### Sprint Backlog

```
┌─────────────────────────────────────────────────────────────────────┐
│ SPRINT 3 BOARD                                                       │
├─────────────────┬─────────────────┬─────────────────┬───────────────┤
│ TODO            │ IN PROGRESS     │ REVIEW          │ DONE          │
├─────────────────┼─────────────────┼─────────────────┼───────────────┤
│ □ 2.1 AIF Clnt  │                 │                 │               │
│ □ 2.2 Cust Acct │                 │                 │               │
│ □ 2.3 Cust Fuzz │                 │                 │               │
└─────────────────┴─────────────────┴─────────────────┴───────────────┘
```

### Definition of Done

- [ ] AIF Client connects to AX with Windows Auth
- [ ] `ax_get_customer` returns customer by account
- [ ] Fuzzy search returns top 5 matches with confidence
- [ ] Integration tests pass against AX test environment

### Dependencies

- AX 2012 test environment available
- AIF services enabled (CustCustomerService)
- Windows Auth configured

---

## Sprint 4: Sales Order Read & Inventory

**Duration:** Week 7-8  
**Epic:** 2 + 3  
**Goal:** Complete read operations for orders and inventory

### Stories

| Story | Title | Priority | Complexity |
|-------|-------|----------|------------|
| 2.4 | Get Sales Order - By Sales ID | P0 | Medium |
| 2.5 | Get Sales Order - With Lines | P0 | Medium |
| 2.6 | Get Sales Order - By Customer | P0 | Medium |
| 3.1 | Check Inventory - Basic | P0 | Medium |
| 3.2 | Check Inventory - By Warehouse | P0 | Low |

### Sprint Backlog

```
┌─────────────────────────────────────────────────────────────────────┐
│ SPRINT 4 BOARD                                                       │
├─────────────────┬─────────────────┬─────────────────┬───────────────┤
│ TODO            │ IN PROGRESS     │ REVIEW          │ DONE          │
├─────────────────┼─────────────────┼─────────────────┼───────────────┤
│ □ 2.4 SO by ID  │                 │                 │               │
│ □ 2.5 SO Lines  │                 │                 │               │
│ □ 2.6 SO by Cst │                 │                 │               │
│ □ 3.1 Inv Basic │                 │                 │               │
│ □ 3.2 Inv WH    │                 │                 │               │
└─────────────────┴─────────────────┴─────────────────┴───────────────┘
```

### Definition of Done

- [ ] `ax_get_salesorder` returns order with lines
- [ ] Order list by customer with pagination
- [ ] `ax_check_inventory` returns availability
- [ ] Warehouse breakdown works correctly
- [ ] All read operations < 500ms (p95)

---

## Sprint 5: Pricing & WCF Setup

**Duration:** Week 9-10  
**Epic:** 3 + 4  
**Goal:** Price simulation and WCF client for writes

### Stories

| Story | Title | Priority | Complexity |
|-------|-------|----------|------------|
| 3.3 | Simulate Price - Basic | P0 | Medium |
| 3.4 | Simulate Price - Date Override | P0 | Low |
| 4.1 | WCF Client Setup | P0 | High |
| 4.2 | Idempotency Store | P0 | Medium |

### Sprint Backlog

```
┌─────────────────────────────────────────────────────────────────────┐
│ SPRINT 5 BOARD                                                       │
├─────────────────┬─────────────────┬─────────────────┬───────────────┤
│ TODO            │ IN PROGRESS     │ REVIEW          │ DONE          │
├─────────────────┼─────────────────┼─────────────────┼───────────────┤
│ □ 3.3 Price     │                 │                 │               │
│ □ 3.4 Price Dt  │                 │                 │               │
│ □ 4.1 WCF Clnt  │                 │                 │               │
│ □ 4.2 Idempot   │                 │                 │               │
└─────────────────┴─────────────────┴─────────────────┴───────────────┘
```

### Definition of Done

- [ ] `ax_simulate_price` returns accurate pricing
- [ ] Trade agreements and discounts applied
- [ ] WCF Client connects with service account
- [ ] Idempotency store prevents duplicates
- [ ] Unit tests for idempotency logic

### Dependencies

- Custom WCF service deployed to AX (GBL_PriceSimulationService)
- Service account provisioned

---

## Sprint 6: Order Creation Complete

**Duration:** Week 11-12  
**Epic:** 4 (Order Creation)  
**Goal:** Full order creation with validation and audit

### Stories

| Story | Title | Priority | Complexity |
|-------|-------|----------|------------|
| 4.3 | Create Sales Order - Input Validation | P0 | Medium |
| 4.4 | Create Sales Order - AX Validation | P0 | High |
| 4.5 | Create Sales Order - Order Creation | P0 | High |
| 4.6 | Create Sales Order - Error Handling | P0 | Medium |
| 4.7 | Create Sales Order - Audit Trail | P0 | Medium |

### Sprint Backlog

```
┌─────────────────────────────────────────────────────────────────────┐
│ SPRINT 6 BOARD                                                       │
├─────────────────┬─────────────────┬─────────────────┬───────────────┤
│ TODO            │ IN PROGRESS     │ REVIEW          │ DONE          │
├─────────────────┼─────────────────┼─────────────────┼───────────────┤
│ □ 4.3 Input Val │                 │                 │               │
│ □ 4.4 AX Valid  │                 │                 │               │
│ □ 4.5 Create SO │                 │                 │               │
│ □ 4.6 Errors    │                 │                 │               │
│ □ 4.7 Audit     │                 │                 │               │
└─────────────────┴─────────────────┴─────────────────┴───────────────┘
```

### Definition of Done

- [ ] `ax_create_salesorder` creates orders in AX
- [ ] All validation rules enforced
- [ ] Idempotency prevents duplicates
- [ ] Errors mapped to user-friendly messages
- [ ] Full audit trail for all operations
- [ ] Order creation < 2s (p95)

### Dependencies

- Custom WCF service deployed (GBL_SalesOrderService)
- Audit database created

---

## Sprint 7: Security & Audit

**Duration:** Week 13-14  
**Epic:** 5 (Security & Audit)  
**Goal:** Production-ready security and compliance

### Stories

| Story | Title | Priority | Complexity |
|-------|-------|----------|------------|
| 5.1 | Windows Authentication | P0 | Medium |
| 5.2 | Role-Based Authorization | P0 | Medium |
| 5.3 | Database Audit Service | P0 | Medium |
| 5.4 | File Audit Service | P0 | Low |
| 5.5 | Audit Query Tool | P1 | Medium |

### Sprint Backlog

```
┌─────────────────────────────────────────────────────────────────────┐
│ SPRINT 7 BOARD                                                       │
├─────────────────┬─────────────────┬─────────────────┬───────────────┤
│ TODO            │ IN PROGRESS     │ REVIEW          │ DONE          │
├─────────────────┼─────────────────┼─────────────────┼───────────────┤
│ □ 5.1 Win Auth  │                 │                 │               │
│ □ 5.2 Roles     │                 │                 │               │
│ □ 5.3 DB Audit  │                 │                 │               │
│ □ 5.4 File Aud  │                 │                 │               │
│ □ 5.5 Aud Query │                 │                 │               │
└─────────────────┴─────────────────┴─────────────────┴───────────────┘
```

### Definition of Done

- [ ] Windows Authentication working
- [ ] Role-based access enforced
- [ ] Write operations logged to database
- [ ] Read operations logged to files
- [ ] `ax_query_audit` returns audit entries
- [ ] Security audit passed

### Dependencies

- AD groups created (MCP-Users-Read, MCP-Users-Write, MCP-Admins)
- Audit database schema deployed

---

## Sprint 8: Monitoring & Production Ready

**Duration:** Week 15-16  
**Epic:** 6 (Resilience & Monitoring)  
**Goal:** Production deployment with full observability

### Stories

| Story | Title | Priority | Complexity |
|-------|-------|----------|------------|
| 6.1 | Full Health Check | P0 | Medium |
| 6.2 | HTTP Health Endpoints | P0 | Low |
| 6.3 | Prometheus Metrics | P1 | Medium |
| 6.4 | Structured Logging | P0 | Low |

### Sprint Backlog

```
┌─────────────────────────────────────────────────────────────────────┐
│ SPRINT 8 BOARD                                                       │
├─────────────────┬─────────────────┬─────────────────┬───────────────┤
│ TODO            │ IN PROGRESS     │ REVIEW          │ DONE          │
├─────────────────┼─────────────────┼─────────────────┼───────────────┤
│ □ 6.1 Full HC   │                 │                 │               │
│ □ 6.2 HTTP HC   │                 │                 │               │
│ □ 6.3 Metrics   │                 │                 │               │
│ □ 6.4 Logging   │                 │                 │               │
└─────────────────┴─────────────────┴─────────────────┴───────────────┘
```

### Definition of Done

- [ ] Full health check verifies all components
- [ ] HTTP endpoints work with load balancer
- [ ] Prometheus metrics exposed
- [ ] JSON structured logging to Seq/ELK
- [ ] Production deployment successful
- [ ] Pilot users onboarded

---

## Release Milestones

| Milestone | Sprint | Date | Criteria |
|-----------|--------|------|----------|
| **M1: Foundation** | Sprint 2 | Week 4 | Server starts, health check works |
| **M2: Read Operations** | Sprint 4 | Week 8 | All read tools functional |
| **M3: Write Operations** | Sprint 6 | Week 12 | Order creation working |
| **M4: Production Ready** | Sprint 8 | Week 16 | Full MVP deployed |

---

## Resource Allocation

| Sprint | .NET Dev | AX Dev | QA | DevOps |
|--------|----------|--------|-----|--------|
| 1 | 2 | 0 | 0.5 | 0.5 |
| 2 | 2 | 0 | 0.5 | 0.5 |
| 3 | 2 | 0.5 | 0.5 | 0 |
| 4 | 2 | 0.5 | 1 | 0 |
| 5 | 2 | 1 | 0.5 | 0 |
| 6 | 2 | 1 | 1 | 0 |
| 7 | 1.5 | 0 | 1 | 0.5 |
| 8 | 1 | 0 | 1 | 1 |

---

## Sprint Ceremonies

| Ceremony | Frequency | Duration | Participants |
|----------|-----------|----------|--------------|
| Sprint Planning | Bi-weekly | 2h | Team + PO |
| Daily Standup | Daily | 15min | Team |
| Sprint Review | Bi-weekly | 1h | Team + Stakeholders |
| Retrospective | Bi-weekly | 1h | Team |
| Backlog Refinement | Weekly | 1h | Team + PO |

---

## Document History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2025-12-06 | Reinerw | Initial Sprint Plan |
