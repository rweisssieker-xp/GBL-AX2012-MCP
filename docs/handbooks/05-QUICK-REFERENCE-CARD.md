---
title: GBL-AX2012-MCP Quick Reference Card
description: One-page cheat sheet for common operations
author: Paige (Technical Writer)
date: 2025-12-07
version: 1.5.0
---

# GBL-AX2012-MCP Quick Reference Card

## 🔧 All 29 Tools at a Glance

### Phase 1: Order Capture

| Tool | Purpose | Example |
|------|---------|---------|
| `ax_health_check` | Check connectivity | "Check AX health" |
| `ax_get_customer` | Get customer info | "Show customer CUST-001" |
| `ax_get_salesorder` | Get order details | "Show order SO-2024-1234" |
| `ax_check_inventory` | Check stock | "Inventory for ITEM-100" |
| `ax_simulate_price` | Get pricing | "Price for CUST-001, ITEM-100, qty 50" |
| `ax_create_salesorder` | Create order | "Create order for CUST-001" |

### Phase 2: Fulfillment

| Tool | Purpose | Example |
|------|---------|---------|
| `ax_reserve_salesline` | Reserve stock | "Reserve line 1 of SO-2024-1234" |
| `ax_post_shipment` | Ship order | "Ship order SO-2024-1234" |
| `ax_release_for_picking` | Release to warehouse | "Release SO-2024-1234 for picking" |

### Phase 3: Invoice & Dunning

| Tool | Purpose | Example |
|------|---------|---------|
| `ax_create_invoice` | Create invoice | "Invoice order SO-2024-1234" |
| `ax_get_customer_aging` | Get AR aging | "Aging for CUST-001" |
| `ax_get_invoice` | Get invoice details | "Show invoice INV-2024-001" |

### Phase 4: Payment & Close

| Tool | Purpose | Example |
|------|---------|---------|
| `ax_post_payment` | Post payment | "Post €1000 for CUST-001" |
| `ax_settle_invoice` | Settle invoice | "Settle INV-001 with PAY-001" |
| `ax_close_salesorder` | Close order | "Close order SO-2024-1234" |

### Master Data & Utilities

| Tool | Purpose | Example |
|------|---------|---------|
| `ax_get_item` | Get item info | "Show item ITEM-100" |
| `ax_update_salesorder` | Update order | "Update delivery date for SO-2024-1234" |
| `ax_add_salesline` | Add order line | "Add ITEM-200 x 25 to SO-2024-1234" |
| `ax_update_delivery_date` | Change delivery | "Change delivery to 2024-12-20" |
| `ax_check_availability_forecast` | Forecast availability | "When is ITEM-100 available for 100 units?" |
| `ax_add_note` | Add note | "Add note to order SO-2024-1234" |
| `ax_check_credit` | Credit check | "Check credit for CUST-001, €75000 order" |
| `ax_query_audit` | Query audit log | "Show audit for today" |

### Approval & Admin

| Tool | Purpose | Example |
|------|---------|---------|
| `ax_request_approval` | Request approval | "Request approval for SO-2024-1234" |
| `ax_get_approval_status` | Check approval | "Status of approval APR-001" |
| `ax_kill_switch` | Emergency stop | "Activate kill switch for writes" |

### P2 Features

| Tool | Purpose | Example |
|------|---------|---------|
| `ax_send_order_confirmation` | Send confirmation email | "Send confirmation for SO-2024-1234" |
| `ax_get_reservation_queue` | Who's waiting for stock? | "Who's waiting for ITEM-100?" |
| `ax_split_order_by_credit` | Split order at credit limit | "Split SO-2024-1234 by credit" |

---

## 🚀 Common Workflows

### Create a Sales Order

```
1. ax_get_customer → Verify customer exists
2. ax_check_inventory → Verify stock available
3. ax_simulate_price → Get pricing
4. ax_create_salesorder → Create the order
```

### Fulfill an Order

```
1. ax_reserve_salesline → Reserve inventory
2. ax_release_for_picking → Release to warehouse
3. ax_post_shipment → Post shipment
```

### Invoice and Collect

```
1. ax_create_invoice → Create invoice
2. ax_get_customer_aging → Monitor aging
3. ax_post_payment → Post payment
4. ax_settle_invoice → Settle invoice
5. ax_close_salesorder → Close order
```

---

## 🔗 HTTP API Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/health` | GET | Health check |
| `/tools` | GET | List all tools |
| `/tools/call` | POST | Execute a tool |
| `/metrics` | GET | Prometheus metrics (port 9090) |

### Example Call

```bash
curl -X POST http://localhost:8080/tools/call \
  -H "Content-Type: application/json" \
  -d '{"tool": "ax_get_customer", "arguments": {"customerAccount": "CUST-001"}}'
```

---

## ⚠️ Error Codes

| Code | Meaning | Action |
|------|---------|--------|
| `CUSTOMER_NOT_FOUND` | Customer doesn't exist | Verify account |
| `CUSTOMER_BLOCKED` | Customer is blocked | Contact Finance |
| `ITEM_BLOCKED` | Item blocked for sales | Check item status |
| `CREDIT_EXCEEDED` | Over credit limit | Request approval |
| `APPROVAL_REQUIRED` | High-value operation | Use approval workflow |
| `RATE_LIMITED` | Too many requests | Wait and retry |
| `CIRCUIT_OPEN` | AX unavailable | Wait for recovery |
| `KILL_SWITCH_ACTIVE` | Emergency stop | Contact Admin |

---

## 📊 Key Metrics

| Metric | Target | Alert |
|--------|--------|-------|
| Availability | 99.5% | < 99% |
| Read Latency (p95) | < 500ms | > 1s |
| Write Latency (p95) | < 2s | > 5s |
| Error Rate | < 2% | > 5% |

---

## 🔐 Roles

| Role | Access |
|------|--------|
| `MCP_Read` | Read-only tools |
| `MCP_Write` | Read + Write tools |
| `MCP_Admin` | All tools + Kill Switch |

---

## 📞 Support

| Issue | Contact |
|-------|---------|
| Access problems | IT Helpdesk |
| AX data questions | Business team |
| System errors | AX Admin team |

---

## 🔧 Useful Commands

```bash
# Health check
curl http://localhost:8080/health

# List tools
curl http://localhost:8080/tools

# Check metrics
curl http://localhost:9090/metrics | grep mcp_

# View logs (Windows)
Get-Content logs\mcp-*.log -Tail 50

# View logs (Linux/Docker)
tail -f logs/mcp-*.log
```

---

*Version 1.5.0 | Print this card for quick reference!*
