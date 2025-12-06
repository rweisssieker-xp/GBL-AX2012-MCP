# Project Roadmap: GBL-AX2012-MCP

**Version:** 1.0  
**Date:** 2025-12-06  
**Author:** Reinerw

---

## Executive Summary

12-month roadmap to deliver full Order-to-Cash automation via MCP Server for AX 2012 R3.

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         GBL-AX2012-MCP ROADMAP                               │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  Q1 2026              Q2 2026              Q3 2026              Q4 2026     │
│  ┌─────────┐          ┌─────────┐          ┌─────────┐          ┌─────────┐ │
│  │ PHASE 1 │          │ PHASE 2 │          │ PHASE 3 │          │ PHASE 4 │ │
│  │   MVP   │    →     │  ORDER  │    →     │FULFILL- │    →     │  FULL   │ │
│  │         │          │  MGMT   │          │  MENT   │          │   O2C   │ │
│  └─────────┘          └─────────┘          └─────────┘          └─────────┘ │
│                                                                              │
│  Month 1-3            Month 4-6            Month 7-9            Month 10-12 │
│  • 6 P0 Tools         • Order Updates      • Picking/Shipping   • Payments  │
│  • Core Infra         • Customer Mgmt      • Invoicing          • Returns   │
│  • Pilot Users        • n8n Integration    • AI-Agent Mode      • Analytics │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Phase 1: MVP (Month 1-3)

### Objective
Deliver core MCP Server with 6 essential tools, validate with pilot users.

### Deliverables

| Sprint | Weeks | Deliverables |
|--------|-------|--------------|
| **Sprint 1** | 1-2 | Project setup, CI/CD, `ax_health_check` |
| **Sprint 2** | 3-4 | `ax_get_customer`, `ax_get_salesorder` |
| **Sprint 3** | 5-6 | `ax_check_inventory`, `ax_simulate_price` |
| **Sprint 4** | 7-8 | `ax_create_salesorder`, integration tests |
| **Sprint 5** | 9-10 | Security hardening, audit logging |
| **Sprint 6** | 11-12 | Pilot deployment, user training |

### Tools Delivered

| Tool | Priority | Complexity |
|------|----------|------------|
| `ax_health_check` | P0 | Low |
| `ax_get_customer` | P0 | Low |
| `ax_get_salesorder` | P0 | Low |
| `ax_check_inventory` | P0 | Low |
| `ax_simulate_price` | P0 | Medium |
| `ax_create_salesorder` | P0 | High |

### Infrastructure

- [x] MCP Server (.NET 8)
- [x] AIF Client (Read operations)
- [x] WCF Client (Write operations)
- [x] BC.NET Client (Admin)
- [x] Rate Limiter
- [x] Circuit Breaker
- [x] Audit Logging
- [x] Health Endpoints

### Success Criteria

| Metric | Target |
|--------|--------|
| All 6 tools functional | 100% |
| Pilot users active | 5 |
| Security audit passed | 0 critical |
| Performance baseline met | p95 <2s |

### Risks

| Risk | Mitigation |
|------|------------|
| AX WCF service development delays | Start early, parallel development |
| Pilot user resistance | Early engagement, training |
| Performance issues | Load testing in Sprint 5 |

---

## Phase 2: Order Management (Month 4-6)

### Objective
Expand to full order lifecycle management, integrate with n8n for automation.

### Deliverables

| Sprint | Weeks | Deliverables |
|--------|-------|--------------|
| **Sprint 7** | 13-14 | `ax_update_salesorder`, `ax_add_salesline` |
| **Sprint 8** | 15-16 | `ax_create_customer`, `ax_update_customer` |
| **Sprint 9** | 17-18 | `ax_reserve_salesline`, `ax_check_credit` |
| **Sprint 10** | 19-20 | n8n integration, first automation flows |
| **Sprint 11** | 21-22 | Rollout to sales team (50%) |
| **Sprint 12** | 23-24 | Monitoring, optimization |

### Tools Delivered

| Tool | Priority | Complexity |
|------|----------|------------|
| `ax_update_salesorder` | P1 | Medium |
| `ax_add_salesline` | P1 | Medium |
| `ax_remove_salesline` | P1 | Medium |
| `ax_create_customer` | P1 | High |
| `ax_update_customer` | P1 | Medium |
| `ax_reserve_salesline` | P1 | High |
| `ax_check_credit` | P1 | Medium |
| `ax_update_credit_limit` | P2 | Medium |

### Automation Flows (n8n)

| Flow | Trigger | Actions |
|------|---------|---------|
| Email Order Processing | New email with order | Parse → Validate → Create Order → Confirm |
| Credit Alert | Credit limit exceeded | Notify Finance → Hold Order |
| Low Stock Alert | Inventory below threshold | Notify SCM → Suggest Reorder |

### Success Criteria

| Metric | Target |
|--------|--------|
| Sales team adoption | >50% |
| Orders via MCP | >30% |
| Time saved per user | 2h/day |
| Error rate reduction | -50% |

---

## Phase 3: Fulfillment (Month 7-9)

### Objective
Automate fulfillment process from picking to invoicing, enable AI-agent autonomous mode.

### Deliverables

| Sprint | Weeks | Deliverables |
|--------|-------|--------------|
| **Sprint 13** | 25-26 | `ax_release_for_picking`, `ax_get_picking_list` |
| **Sprint 14** | 27-28 | `ax_post_packingslip`, `ax_create_shipment` |
| **Sprint 15** | 29-30 | `ax_post_invoice`, `ax_get_invoice` |
| **Sprint 16** | 31-32 | AI-Agent autonomous mode |
| **Sprint 17** | 33-34 | SCM team rollout |
| **Sprint 18** | 35-36 | Finance team rollout |

### Tools Delivered

| Tool | Priority | Complexity |
|------|----------|------------|
| `ax_release_for_picking` | P1 | Medium |
| `ax_get_picking_list` | P1 | Low |
| `ax_confirm_picking` | P1 | Medium |
| `ax_post_packingslip` | P1 | High |
| `ax_create_shipment` | P1 | Medium |
| `ax_post_invoice` | P1 | High |
| `ax_get_invoice` | P1 | Low |

### AI-Agent Mode

```
┌─────────────────────────────────────────────────────────────────────┐
│                    AI-AGENT AUTONOMOUS MODE                          │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌─────────┐    ┌─────────┐    ┌─────────┐    ┌─────────┐          │
│  │ TRIGGER │ →  │ VALIDATE│ →  │ EXECUTE │ →  │ CONFIRM │          │
│  └─────────┘    └─────────┘    └─────────┘    └─────────┘          │
│       │              │              │              │                 │
│       ▼              ▼              ▼              ▼                 │
│  Email/EDI      Confidence     Auto-Execute   Send Confirm         │
│  Webshop        Score >95%     if Valid       to Customer          │
│                      │                                              │
│                      ▼                                              │
│                 ┌─────────┐                                         │
│                 │ESCALATE │ ← Confidence <95%                       │
│                 │ to Human│                                         │
│                 └─────────┘                                         │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘
```

### Success Criteria

| Metric | Target |
|--------|--------|
| Fulfillment automation | >50% |
| Invoice automation | >40% |
| AI-Agent success rate | >95% |
| Escalation rate | <5% |

---

## Phase 4: Full O2C + Advanced (Month 10-12)

### Objective
Complete Order-to-Cash cycle, add analytics and advanced features.

### Deliverables

| Sprint | Weeks | Deliverables |
|--------|-------|--------------|
| **Sprint 19** | 37-38 | `ax_post_payment`, `ax_get_payment_status` |
| **Sprint 20** | 39-40 | `ax_create_return_order`, `ax_process_return` |
| **Sprint 21** | 41-42 | `ax_get_sales_analytics`, dashboards |
| **Sprint 22** | 43-44 | `ax_get_availability_forecast` |
| **Sprint 23** | 45-46 | Multi-company support |
| **Sprint 24** | 47-48 | Documentation, handover |

### Tools Delivered

| Tool | Priority | Complexity |
|------|----------|------------|
| `ax_post_payment` | P2 | High |
| `ax_get_payment_status` | P2 | Low |
| `ax_create_return_order` | P2 | High |
| `ax_process_return` | P2 | High |
| `ax_get_sales_analytics` | P2 | Medium |
| `ax_get_availability_forecast` | P2 | Medium |

### Success Criteria

| Metric | Target |
|--------|--------|
| Full O2C coverage | 100% |
| End-to-end automation | >60% |
| Cost savings | Measured |
| D365 migration ready | Interface abstracted |

---

## Resource Plan

### Team Composition

| Role | FTE | Phase 1 | Phase 2 | Phase 3 | Phase 4 |
|------|-----|---------|---------|---------|---------|
| Tech Lead | 1.0 | ✓ | ✓ | ✓ | ✓ |
| .NET Developer | 2.0 | ✓ | ✓ | ✓ | ✓ |
| AX Developer | 1.0 | ✓ | ✓ | ✓ | 0.5 |
| QA Engineer | 1.0 | ✓ | ✓ | ✓ | ✓ |
| DevOps | 0.5 | ✓ | ✓ | ✓ | ✓ |
| Product Owner | 0.5 | ✓ | ✓ | ✓ | ✓ |

### Budget Estimate

| Category | Phase 1 | Phase 2 | Phase 3 | Phase 4 | Total |
|----------|---------|---------|---------|---------|-------|
| Development | €60k | €60k | €60k | €60k | €240k |
| Infrastructure | €5k | €5k | €5k | €5k | €20k |
| Licenses | €10k | €0 | €0 | €0 | €10k |
| Training | €5k | €5k | €5k | €5k | €20k |
| **Total** | **€80k** | **€70k** | **€70k** | **€70k** | **€290k** |

---

## Milestones

| Milestone | Date | Criteria |
|-----------|------|----------|
| **M1: MVP Ready** | Month 3 | 6 tools functional, pilot started |
| **M2: Sales Rollout** | Month 6 | 50% sales adoption |
| **M3: Fulfillment Live** | Month 9 | Picking to invoice automated |
| **M4: Full O2C** | Month 12 | Complete cycle, >60% automation |

---

## Dependencies

| Dependency | Owner | Status | Risk |
|------------|-------|--------|------|
| AX 2012 R3 CU13 environment | IT | ✓ Available | Low |
| AIF Services enabled | AX Admin | ✓ Available | Low |
| Custom WCF service development | AX Dev | In Progress | Medium |
| Service account provisioning | IT Security | Pending | Low |
| n8n instance | IT | Pending | Low |
| Pilot user availability | Business | Confirmed | Low |

---

## Risk Register

| # | Risk | Probability | Impact | Mitigation |
|---|------|-------------|--------|------------|
| R1 | AX WCF development delays | Medium | High | Parallel development, early start |
| R2 | User adoption resistance | Medium | Medium | Early engagement, training, champions |
| R3 | Performance issues | Low | High | Load testing, dedicated AOS |
| R4 | Security vulnerabilities | Low | Critical | Security audit, penetration testing |
| R5 | Scope creep | High | Medium | Strict MVP, change control |
| R6 | Key person dependency | Medium | High | Documentation, knowledge sharing |

---

## Communication Plan

| Audience | Frequency | Format | Owner |
|----------|-----------|--------|-------|
| Steering Committee | Monthly | Status Report | Product Owner |
| Project Team | Weekly | Stand-up | Tech Lead |
| Stakeholders | Bi-weekly | Demo | Product Owner |
| Pilot Users | Weekly | Feedback Session | Product Owner |
| IT Operations | As needed | Technical Briefing | Tech Lead |

---

## Document History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2025-12-06 | Reinerw | Initial roadmap |
