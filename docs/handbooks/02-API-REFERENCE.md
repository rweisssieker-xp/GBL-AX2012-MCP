---
title: GBL-AX2012-MCP API Reference
description: Complete API documentation for all MCP tools
author: Paige (Technical Writer)
date: 2025-12-06
version: 1.4.0
---

# GBL-AX2012-MCP API Reference

## Overview

This document provides complete API documentation for all 26 tools available in the GBL-AX2012-MCP server.

### Transport Options

| Transport | Endpoint | Use Case |
|-----------|----------|----------|
| **stdio** | Standard I/O | Claude Desktop, MCP clients |
| **HTTP** | `http://localhost:8080` | n8n, webhooks, REST clients |
| **Metrics** | `http://localhost:9090/metrics` | Prometheus monitoring |

---

## HTTP API Endpoints

### List Available Tools

```http
GET /tools
```

**Response:**

```json
{
  "tools": [
    {
      "name": "ax_health_check",
      "description": "Check server and AX connectivity",
      "inputSchema": { ... }
    }
  ]
}
```

### Call a Tool

```http
POST /tools/call
Content-Type: application/json

{
  "tool": "ax_get_customer",
  "arguments": {
    "customerAccount": "CUST-001"
  }
}
```

### Health Check

```http
GET /health
```

**Response:**

```json
{
  "status": "healthy",
  "timestamp": "2024-12-06T10:30:00Z",
  "components": {
    "aif": "connected",
    "wcf": "connected",
    "bc": "connected"
  }
}
```

---

## Tool Reference

### Phase 1: Order Capture

#### ax_health_check

Check server and AX connectivity status.

**Input:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `detailed` | boolean | No | Include component-level details |

**Output:**

```json
{
  "status": "healthy",
  "aosConnected": true,
  "responseTimeMs": 45,
  "components": {
    "aif": "connected",
    "wcf": "connected",
    "businessConnector": "connected"
  }
}
```

---

#### ax_get_customer

Retrieve customer information by account or search by name.

**Input:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `customerAccount` | string | No* | Exact customer account |
| `searchName` | string | No* | Partial name search |
| `includeBlocked` | boolean | No | Include blocked customers |

*One of `customerAccount` or `searchName` is required.

**Output:**

```json
{
  "customerAccount": "CUST-001",
  "name": "Müller GmbH",
  "address": "Hauptstraße 1, 12345 Berlin",
  "creditLimit": 100000.00,
  "balance": 25000.00,
  "blocked": false,
  "currency": "EUR",
  "paymentTerms": "Net30"
}
```

**Errors:**

| Code | Description |
|------|-------------|
| `CUSTOMER_NOT_FOUND` | No customer matches the criteria |
| `CUSTOMER_BLOCKED` | Customer is blocked for transactions |

---

#### ax_get_salesorder

Retrieve sales order details or list orders by customer.

**Input:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `salesId` | string | No* | Specific sales order ID |
| `customerAccount` | string | No* | List orders for customer |
| `statusFilter` | string[] | No | Filter by status |
| `includeLines` | boolean | No | Include order lines |

**Output:**

```json
{
  "salesId": "SO-2024-1234",
  "customerAccount": "CUST-001",
  "customerName": "Müller GmbH",
  "orderDate": "2024-12-01",
  "requestedDelivery": "2024-12-15",
  "status": "Open",
  "totalAmount": 1125.00,
  "currency": "EUR",
  "lines": [
    {
      "lineNum": 1,
      "itemId": "ITEM-100",
      "itemName": "Widget Pro",
      "quantity": 50,
      "unitPrice": 22.50,
      "lineAmount": 1125.00,
      "reservedQty": 50,
      "deliveredQty": 0
    }
  ]
}
```

---

#### ax_check_inventory

Check item availability across warehouses.

**Input:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `itemId` | string | Yes | Item identifier |
| `warehouseId` | string | No | Specific warehouse |

**Output:**

```json
{
  "itemId": "ITEM-100",
  "itemName": "Widget Pro",
  "totalOnHand": 500,
  "available": 350,
  "reserved": 150,
  "onOrder": 200,
  "warehouses": [
    {
      "warehouseId": "WH-MAIN",
      "warehouseName": "Main Warehouse",
      "onHand": 300,
      "available": 200,
      "reserved": 100
    }
  ]
}
```

---

#### ax_simulate_price

Calculate pricing without creating an order.

**Input:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `customerAccount` | string | Yes | Customer for pricing |
| `itemId` | string | Yes | Item to price |
| `quantity` | decimal | Yes | Quantity for pricing |
| `unitId` | string | No | Unit of measure |

**Output:**

```json
{
  "basePrice": 25.00,
  "customerDiscountPct": 10.0,
  "quantityDiscountPct": 0.0,
  "finalUnitPrice": 22.50,
  "lineAmount": 1125.00,
  "currency": "EUR",
  "priceSource": "CustomerPriceGroup",
  "validUntil": "2024-12-31"
}
```

---

#### ax_create_salesorder

Create a new sales order.

**Input:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `customerAccount` | string | Yes | Customer account |
| `requestedDeliveryDate` | date | No | Requested delivery |
| `customerRef` | string | No | Customer reference |
| `lines` | array | Yes | Order lines |
| `lines[].itemId` | string | Yes | Item ID |
| `lines[].quantity` | decimal | Yes | Quantity |
| `lines[].unitPrice` | decimal | No | Override price |
| `lines[].warehouseId` | string | No | Source warehouse |

**Output:**

```json
{
  "salesId": "SO-2024-1234",
  "customerAccount": "CUST-001",
  "status": "Open",
  "totalAmount": 1125.00,
  "lineCount": 1,
  "requiresApproval": false
}
```

**Errors:**

| Code | Description |
|------|-------------|
| `CUSTOMER_BLOCKED` | Customer is blocked |
| `ITEM_BLOCKED` | Item is blocked for sales |
| `CREDIT_EXCEEDED` | Order exceeds credit limit |
| `APPROVAL_REQUIRED` | Order requires approval |

---

### Phase 2: Fulfillment

#### ax_reserve_salesline

Reserve inventory for a sales order line.

**Input:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `salesId` | string | Yes | Sales order ID |
| `lineNum` | integer | Yes | Line number |
| `quantity` | decimal | No | Quantity to reserve |
| `warehouseId` | string | No | Source warehouse |

**Output:**

```json
{
  "salesId": "SO-2024-1234",
  "lineNum": 1,
  "itemId": "ITEM-100",
  "reservedQuantity": 50,
  "warehouseId": "WH-MAIN",
  "status": "Reserved"
}
```

---

#### ax_post_shipment

Post shipment/packing slip for a sales order.

**Input:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `salesId` | string | Yes | Sales order ID |
| `shipDate` | date | No | Ship date (default: today) |
| `lineNums` | integer[] | No | Specific lines to ship |

**Output:**

```json
{
  "salesId": "SO-2024-1234",
  "packingSlipId": "PS-2024-5678",
  "shipDate": "2024-12-06",
  "linesShipped": 1,
  "totalQuantity": 50
}
```

---

### Phase 3: Invoice & Dunning

#### ax_create_invoice

Create and post an invoice for a sales order.

**Input:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `salesId` | string | Yes | Sales order ID |
| `invoiceDate` | date | No | Invoice date |

**Output:**

```json
{
  "invoiceId": "INV-2024-9012",
  "salesId": "SO-2024-1234",
  "customerAccount": "CUST-001",
  "invoiceDate": "2024-12-06",
  "dueDate": "2025-01-05",
  "amount": 1125.00,
  "currency": "EUR"
}
```

---

#### ax_get_customer_aging

Get accounts receivable aging report for a customer.

**Input:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `customerAccount` | string | Yes | Customer account |
| `asOfDate` | date | No | Aging as of date |

**Output:**

```json
{
  "customerAccount": "CUST-001",
  "customerName": "Müller GmbH",
  "asOfDate": "2024-12-06",
  "totalOpen": 700.00,
  "currency": "EUR",
  "buckets": {
    "current": 500.00,
    "days1to30": 200.00,
    "days31to60": 0.00,
    "days61to90": 0.00,
    "over90": 0.00
  },
  "openInvoices": [
    {
      "invoiceId": "INV-2024-8901",
      "invoiceDate": "2024-11-15",
      "dueDate": "2024-12-15",
      "amount": 500.00,
      "openAmount": 500.00
    }
  ]
}
```

---

### Phase 4: Payment & Close

#### ax_post_payment

Post a customer payment.

**Input:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `customerAccount` | string | Yes | Customer account |
| `amount` | decimal | Yes | Payment amount |
| `paymentDate` | date | No | Payment date |
| `paymentReference` | string | No | Bank reference |
| `paymentMethod` | string | No | Payment method |

**Output:**

```json
{
  "paymentId": "PAY-2024-3456",
  "customerAccount": "CUST-001",
  "amount": 1125.00,
  "paymentDate": "2024-12-06",
  "status": "Posted"
}
```

---

#### ax_settle_invoice

Settle an invoice against a payment.

**Input:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `invoiceId` | string | Yes | Invoice to settle |
| `paymentId` | string | Yes | Payment to apply |
| `amount` | decimal | No | Partial settlement |

**Output:**

```json
{
  "settlementId": "SET-2024-7890",
  "invoiceId": "INV-2024-9012",
  "paymentId": "PAY-2024-3456",
  "settledAmount": 1125.00,
  "invoiceRemaining": 0.00,
  "paymentRemaining": 0.00
}
```

---

#### ax_close_salesorder

Close a completed sales order.

**Input:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `salesId` | string | Yes | Sales order ID |
| `reason` | string | No | Closure reason |

**Output:**

```json
{
  "salesId": "SO-2024-1234",
  "previousStatus": "Invoiced",
  "newStatus": "Closed",
  "closedDate": "2024-12-06"
}
```

---

### Approval Workflow

#### ax_request_approval

Request approval for a high-value operation.

**Input:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `operationType` | string | Yes | Type of operation |
| `referenceId` | string | Yes | Reference (e.g., SalesId) |
| `amount` | decimal | Yes | Amount requiring approval |
| `reason` | string | No | Justification |

**Output:**

```json
{
  "approvalId": "APR-2024-001",
  "status": "Pending",
  "requestedBy": "DOMAIN\\user",
  "requestedAt": "2024-12-06T10:30:00Z",
  "approverQueue": "Finance"
}
```

---

#### ax_get_approval_status

Check the status of an approval request.

**Input:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `approvalId` | string | Yes | Approval request ID |

**Output:**

```json
{
  "approvalId": "APR-2024-001",
  "status": "Approved",
  "requestedBy": "DOMAIN\\user",
  "requestedAt": "2024-12-06T10:30:00Z",
  "decidedBy": "DOMAIN\\approver",
  "decidedAt": "2024-12-06T11:00:00Z",
  "comments": "Approved per customer agreement"
}
```

---

### Master Data & Utilities

#### ax_get_item

Retrieve item master data.

**Input:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `itemId` | string | No* | Exact item ID |
| `searchName` | string | No* | Search by name |
| `itemGroup` | string | No | Filter by group |

**Output:**

```json
{
  "itemId": "ITEM-100",
  "name": "Widget Pro",
  "itemGroup": "WIDGETS",
  "unit": "PCS",
  "blocked": false,
  "inventory": {
    "onHand": 500,
    "available": 350,
    "reserved": 150
  }
}
```

---

#### ax_update_salesorder

Update an existing sales order.

**Input:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `salesId` | string | Yes | Sales order ID |
| `requestedDeliveryDate` | date | No | New delivery date |
| `customerRef` | string | No | Customer reference |

**Output:**

```json
{
  "salesId": "SO-2024-1234",
  "updated": true,
  "changes": ["requestedDeliveryDate"]
}
```

---

#### ax_get_invoice

Retrieve invoice details.

**Input:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `invoiceId` | string | No* | Specific invoice |
| `salesId` | string | No* | Invoices for order |
| `customerAccount` | string | No* | Invoices for customer |
| `includeLines` | boolean | No | Include line details |

**Output:**

```json
{
  "invoiceId": "INV-2024-9012",
  "salesId": "SO-2024-1234",
  "customerAccount": "CUST-001",
  "invoiceDate": "2024-12-06",
  "dueDate": "2025-01-05",
  "amount": 1125.00,
  "paidAmount": 0.00,
  "openAmount": 1125.00,
  "status": "Open"
}
```

---

#### ax_add_note

Add a note to an entity (customer, order, invoice).

**Input:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `entityType` | string | Yes | customer, salesorder, invoice |
| `entityId` | string | Yes | Entity identifier |
| `noteText` | string | Yes | Note content |
| `noteType` | string | No | internal, external |

**Output:**

```json
{
  "noteId": "NOTE-2024-001",
  "entityType": "salesorder",
  "entityId": "SO-2024-1234",
  "createdAt": "2024-12-06T10:30:00Z",
  "createdBy": "DOMAIN\\user"
}
```

---

#### ax_check_credit

Check customer credit status for a potential order.

**Input:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `customerAccount` | string | Yes | Customer account |
| `orderAmount` | decimal | Yes | Proposed order amount |

**Output:**

```json
{
  "customerAccount": "CUST-001",
  "creditLimit": 100000.00,
  "currentBalance": 25000.00,
  "openOrders": 15000.00,
  "availableCredit": 60000.00,
  "proposedOrder": 75000.00,
  "wouldExceed": true,
  "recommendation": "Request approval or reduce order"
}
```

---

#### ax_query_audit

Query the audit log.

**Input:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `toolName` | string | No | Filter by tool |
| `user` | string | No | Filter by user |
| `fromDate` | date | No | Start date |
| `toDate` | date | No | End date |
| `successOnly` | boolean | No | Only successful calls |
| `maxResults` | integer | No | Limit results |

**Output:**

```json
{
  "totalCount": 150,
  "entries": [
    {
      "id": "abc123",
      "timestamp": "2024-12-06T10:30:00Z",
      "toolName": "ax_create_salesorder",
      "user": "DOMAIN\\user",
      "success": true,
      "durationMs": 245
    }
  ],
  "summary": {
    "successRate": 98.5,
    "avgDurationMs": 180
  }
}
```

---

### Extended Features

#### ax_check_availability_forecast

Forecast when an item will be available.

**Input:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `itemId` | string | Yes | Item ID |
| `requiredQuantity` | decimal | Yes | Quantity needed |
| `warehouseId` | string | No | Specific warehouse |

**Output:**

```json
{
  "itemId": "ITEM-100",
  "requiredQuantity": 100,
  "currentAvailable": 50,
  "shortfall": 50,
  "estimatedAvailableDate": "2024-12-20",
  "incomingSupply": [
    {
      "date": "2024-12-15",
      "quantity": 30,
      "source": "PO-2024-100"
    },
    {
      "date": "2024-12-20",
      "quantity": 50,
      "source": "PO-2024-101"
    }
  ],
  "recommendation": "Full quantity available by 2024-12-20"
}
```

---

#### ax_update_delivery_date

Update the delivery date for a sales order.

**Input:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `salesId` | string | Yes | Sales order ID |
| `newDeliveryDate` | date | Yes | New delivery date |
| `reason` | string | No | Reason for change |
| `notifyCustomer` | boolean | No | Send notification |

**Output:**

```json
{
  "salesId": "SO-2024-1234",
  "oldDeliveryDate": "2024-12-10",
  "newDeliveryDate": "2024-12-15",
  "success": true,
  "customerNotified": true
}
```

---

#### ax_add_salesline

Add a new line to an existing sales order.

**Input:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `salesId` | string | Yes | Sales order ID |
| `itemId` | string | Yes | Item to add |
| `quantity` | decimal | Yes | Quantity |
| `unitPrice` | decimal | No | Override price |
| `warehouseId` | string | No | Source warehouse |

**Output:**

```json
{
  "salesId": "SO-2024-1234",
  "lineNum": 2,
  "itemId": "ITEM-200",
  "quantity": 25,
  "unitPrice": 15.00,
  "lineAmount": 375.00,
  "newOrderTotal": 1500.00
}
```

---

#### ax_release_for_picking

Release a sales order for warehouse picking.

**Input:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `salesId` | string | Yes | Sales order ID |
| `lineNums` | integer[] | No | Specific lines |
| `warehouseId` | string | No | Target warehouse |

**Output:**

```json
{
  "salesId": "SO-2024-1234",
  "releasedLines": [1, 2],
  "pickingListId": "PL-2024-001",
  "status": "ReleasedForPicking"
}
```

---

### Admin Tools

#### ax_kill_switch

Emergency stop for all or specific tools.

**Input:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `action` | string | Yes | activate, deactivate, status |
| `scope` | string | No | all, writes, specific tool |
| `reason` | string | Yes* | Required for activate |

**Output:**

```json
{
  "killSwitchActive": true,
  "scope": "writes",
  "activatedBy": "DOMAIN\\admin",
  "activatedAt": "2024-12-06T10:30:00Z",
  "reason": "Emergency maintenance"
}
```

---

## Error Codes

| Code | HTTP Status | Description |
|------|-------------|-------------|
| `VALIDATION_ERROR` | 400 | Input validation failed |
| `NOT_FOUND` | 404 | Resource not found |
| `CUSTOMER_BLOCKED` | 403 | Customer is blocked |
| `ITEM_BLOCKED` | 403 | Item is blocked |
| `CREDIT_EXCEEDED` | 403 | Credit limit exceeded |
| `APPROVAL_REQUIRED` | 403 | Approval needed |
| `UNAUTHORIZED` | 401 | Authentication failed |
| `FORBIDDEN` | 403 | Insufficient permissions |
| `RATE_LIMITED` | 429 | Too many requests |
| `CIRCUIT_OPEN` | 503 | Circuit breaker open |
| `AX_ERROR` | 502 | AX system error |
| `KILL_SWITCH_ACTIVE` | 503 | Kill switch engaged |

---

## Rate Limits

| Scope | Limit | Window |
|-------|-------|--------|
| Per User | 100 requests | 1 minute |
| Per Tool | 50 requests | 1 minute |
| Write Operations | 20 requests | 1 minute |

---

## Authentication

All requests require Windows Authentication (NTLM/Kerberos).

**HTTP Header:**
```
Authorization: Negotiate <token>
```

**Roles:**

| Role | Permissions |
|------|-------------|
| `MCP_Read` | Read-only tools |
| `MCP_Write` | Read + Write tools |
| `MCP_Admin` | All tools + Kill Switch |

---

*Document Version: 1.4.0 | Last Updated: 2025-12-06*
