---
stepsCompleted: [0, 1, 2, 3]
inputDocuments:
  - "docs/requirements/prd-GBL-AX2012-MCP-2025-12-06.md"
  - "docs/architecture/technical-spec-GBL-AX2012-MCP-2025-12-06.md"
workflowType: "create-epics-and-stories"
status: "COMPLETED"
project_name: "GBL-AX2012-MCP"
user_name: "Reinerw"
date: "2025-12-06"
---

# GBL-AX2012-MCP - Epic Breakdown

**Author:** Reinerw  
**Date:** 2025-12-06  
**Project Level:** Enterprise Integration  
**Target Scale:** 20 concurrent users, 100 ops/min

---

## Overview

This document provides the complete epic and story breakdown for GBL-AX2012-MCP, decomposing the requirements from the PRD into implementable stories with full technical context from the Architecture specification.

### Epic Summary

| Epic | Title | Stories | FRs Covered |
|------|-------|---------|-------------|
| **1** | Foundation & Infrastructure | 8 | NFR-001 to NFR-004 |
| **2** | Read Operations (Customer & Orders) | 6 | FR-001, FR-002, FR-003 |
| **3** | Inventory & Pricing | 4 | FR-004, FR-005 |
| **4** | Order Creation & Validation | 7 | FR-006 |
| **5** | Security & Audit | 5 | NFR-003, US-006, US-007 |
| **6** | Resilience & Monitoring | 4 | NFR-004, NFR-001, NFR-002 |
| **7** | Batch Operations & Webhooks | 6 | New Requirements | **NEW** |
| **8** | Zero-Downtime & Self-Healing | 6 | NFR-002 (Enhanced), NFR-004 (Enhanced) | **NEW** |

**Total:** 34 Stories across 6 Epics

---

## Functional Requirements Inventory

### Functional Requirements (FRs)

| FR | Description | Priority |
|----|-------------|----------|
| FR-001 | Health Check Tool (`ax_health_check`) | P0 |
| FR-002 | Get Customer Tool (`ax_get_customer`) | P0 |
| FR-003 | Get Sales Order Tool (`ax_get_salesorder`) | P0 |
| FR-004 | Check Inventory Tool (`ax_check_inventory`) | P0 |
| FR-005 | Simulate Price Tool (`ax_simulate_price`) | P0 |
| FR-006 | Create Sales Order Tool (`ax_create_salesorder`) | P0 |

### Non-Functional Requirements (NFRs)

| NFR | Description | Target |
|-----|-------------|--------|
| NFR-001 | Performance | Read <500ms, Write <2s (p95) |
| NFR-002 | Availability | 99.5% uptime |
| NFR-003 | Security | Windows Auth, Role-based, Audit |
| NFR-004 | Resilience | Circuit Breaker, Rate Limiting, Retry |

### User Stories from PRD

| US | Description | Epic |
|----|-------------|------|
| US-001 | Quick Order Entry | Epic 4 |
| US-002 | Customer Lookup | Epic 2 |
| US-003 | Order Status Check | Epic 2 |
| US-004 | Price Quote | Epic 3 |
| US-005 | Inventory Check | Epic 3 |
| US-006 | Health Monitoring | Epic 5 |
| US-007 | Audit Trail | Epic 5 |

---

## FR Coverage Map

```
FR-001 (Health Check)     → Epic 1.8, Epic 6.1
FR-002 (Get Customer)     → Epic 2.1, 2.2, 2.3
FR-003 (Get Sales Order)  → Epic 2.4, 2.5, 2.6
FR-004 (Check Inventory)  → Epic 3.1, 3.2
FR-005 (Simulate Price)   → Epic 3.3, 3.4
FR-006 (Create Order)     → Epic 4.1-4.7

NFR-001 (Performance)     → Epic 1.3, 6.2
NFR-002 (Availability)    → Epic 6.3, 6.4
NFR-003 (Security)        → Epic 5.1-5.5
NFR-004 (Resilience)      → Epic 1.5, 1.6, 6.1-6.4
```

---

## Epic 1: Foundation & Infrastructure

**Goal:** Establish the core MCP Server infrastructure, project structure, and integration foundation. After this epic, the server can start, connect to AX, and respond to basic requests.

**User Value:** IT team can deploy and verify the MCP Server connects to AX 2012 successfully.

**FRs Covered:** NFR-001, NFR-002, NFR-003, NFR-004 (foundation)  
**Architecture Sections:** 2.1, 2.2, 5.1, 5.2

---

### Story 1.1: Project Setup & Solution Structure

As a **developer**,  
I want a properly structured .NET 8 solution with all projects and dependencies,  
So that I can start implementing features immediately.

**Acceptance Criteria:**

**Given** I clone the repository  
**When** I open the solution in Visual Studio/Rider  
**Then** I see the following project structure:
```
GBL.AX2012.MCP/
├── src/
│   ├── GBL.AX2012.MCP.Server/
│   ├── GBL.AX2012.MCP.AxConnector/
│   ├── GBL.AX2012.MCP.Core/
│   └── GBL.AX2012.MCP.Audit/
├── tests/
│   ├── GBL.AX2012.MCP.Server.Tests/
│   ├── GBL.AX2012.MCP.AxConnector.Tests/
│   └── GBL.AX2012.MCP.Integration.Tests/
└── deploy/
```

**And** all NuGet packages are restored successfully  
**And** the solution builds without errors  
**And** `dotnet test` runs (even if no tests exist yet)

**Technical Notes:**
- Target .NET 8.0 LTS
- Use `Microsoft.Extensions.DependencyInjection` for DI
- Use `Microsoft.Extensions.Configuration` for config
- Use `Serilog` for logging
- Reference Architecture Section 2.1

**Prerequisites:** None (first story)

---

### Story 1.2: Configuration System

As a **developer**,  
I want a configuration system that loads settings from appsettings.json,  
So that I can configure the server without code changes.

**Acceptance Criteria:**

**Given** the MCP Server starts  
**When** it loads configuration  
**Then** it reads from `appsettings.json` and `appsettings.{Environment}.json`

**And** the following configuration sections are available:
- `McpServer` (Transport, ServerName, ServerVersion)
- `AifClient` (BaseUrl, Timeout)
- `WcfClient` (BaseUrl, ServiceAccountUser, ServiceAccountDomain, Timeout)
- `BusinessConnector` (ObjectServer, Company, Language)
- `RateLimiter` (RequestsPerMinute, Enabled)
- `CircuitBreaker` (FailureThreshold, OpenDuration, Timeout)
- `Audit` (DatabaseConnectionString, FileLogPath, RetentionDays)
- `Security` (RequireAuthentication, AllowedRoles, ApprovalThreshold)

**And** configuration is validated at startup  
**And** missing required settings throw clear error messages

**Technical Notes:**
- Use `IOptions<T>` pattern for strongly-typed config
- Implement `IValidateOptions<T>` for validation
- Reference Architecture Section 5.1

**Prerequisites:** Story 1.1

---

### Story 1.3: Dependency Injection Setup

As a **developer**,  
I want all services registered in the DI container,  
So that dependencies are properly injected throughout the application.

**Acceptance Criteria:**

**Given** the MCP Server starts  
**When** the DI container is built  
**Then** the following services are registered:
- `IMcpServer` (Singleton)
- `IAifClient` (Scoped)
- `IWcfClient` (Scoped)
- `IBusinessConnector` (Singleton)
- `IRateLimiter` (Singleton)
- `ICircuitBreaker` (Singleton)
- `IAuditService` (Scoped)
- `IIdempotencyStore` (Singleton)
- All `ITool` implementations (Scoped)

**And** services can be resolved without errors  
**And** scoped services are properly disposed

**Technical Notes:**
- Create `ServiceCollectionExtensions.cs` in Core project
- Use `AddSingleton`, `AddScoped`, `AddTransient` appropriately
- Reference Architecture Section 2.2.1

**Prerequisites:** Story 1.1, 1.2

---

### Story 1.4: MCP Server Host

As a **developer**,  
I want the MCP Server to run as a hosted service,  
So that it can be started and stopped gracefully.

**Acceptance Criteria:**

**Given** I run `dotnet run` in the Server project  
**When** the application starts  
**Then** the MCP Server starts listening on the configured transport (stdio)

**And** I see log message: "Starting MCP Server on {Transport}"  
**And** the server responds to MCP protocol initialization  
**And** pressing Ctrl+C triggers graceful shutdown  
**And** I see log message: "Stopping MCP Server"

**Technical Notes:**
- Implement `IHostedService` for `McpServer`
- Use `IHostApplicationLifetime` for shutdown handling
- Reference Architecture Section 2.2.1

**Prerequisites:** Story 1.3

---

### Story 1.5: Rate Limiter Implementation

As a **developer**,  
I want a rate limiter that restricts requests per user,  
So that AX is protected from overload.

**Acceptance Criteria:**

**Given** rate limiting is enabled (config: `RateLimiter.Enabled = true`)  
**When** a user makes requests  
**Then** requests are allowed up to `RequestsPerMinute` (default: 100)

**And** when the limit is exceeded, the request is rejected  
**And** the response includes `RATE_LIMITED` error code  
**And** the response includes `retry_after_seconds` field  
**And** the rate limit resets after 1 minute

**Technical Notes:**
- Implement Token Bucket algorithm
- Use `ConcurrentDictionary` for per-user buckets
- Reference Architecture Section 4.1

**Prerequisites:** Story 1.3

---

### Story 1.6: Circuit Breaker Implementation

As a **developer**,  
I want a circuit breaker that prevents cascading failures,  
So that the system remains responsive when AX is unhealthy.

**Acceptance Criteria:**

**Given** the circuit breaker is in CLOSED state  
**When** 3 consecutive failures occur (configurable)  
**Then** the circuit breaker transitions to OPEN state

**And** all requests are immediately rejected with `CIRCUIT_OPEN` error  
**And** after 60 seconds (configurable), the circuit transitions to HALF-OPEN  
**And** one test request is allowed through  
**And** if the test succeeds, the circuit transitions to CLOSED  
**And** if the test fails, the circuit returns to OPEN

**Technical Notes:**
- Implement state machine: CLOSED → OPEN → HALF-OPEN → CLOSED
- Use `Stopwatch` for timing
- Reference Architecture Section 4.2

**Prerequisites:** Story 1.3

---

### Story 1.7: Tool Base Class

As a **developer**,  
I want a base class for all MCP tools,  
So that common functionality (validation, audit, error handling) is consistent.

**Acceptance Criteria:**

**Given** I create a new tool by inheriting from `ToolBase<TInput, TOutput>`  
**When** the tool is executed  
**Then** input is automatically deserialized from JSON

**And** input is validated using `IValidator<TInput>`  
**And** execution time is measured  
**And** audit entry is created with user, timestamp, input, output  
**And** exceptions are caught and logged  
**And** response is serialized to JSON

**Technical Notes:**
- Use generics for type-safe input/output
- Use FluentValidation for validators
- Reference Architecture Section 2.2.2

**Prerequisites:** Story 1.3

---

### Story 1.8: Health Check Tool (Basic)

As an **IT Administrator**,  
I want to check if the MCP Server is running,  
So that I can verify the deployment is successful.

**Acceptance Criteria:**

**Given** the MCP Server is running  
**When** I call `ax_health_check` with no parameters  
**Then** I receive a response within 5 seconds

**And** the response includes:
```json
{
  "status": "healthy",
  "timestamp": "2025-12-06T14:00:00Z",
  "server_version": "1.0.0"
}
```

**And** if the server is starting up, status is "starting"  
**And** if there are issues, status is "degraded" or "unhealthy"

**Technical Notes:**
- This is a basic health check (server only)
- Full AX connectivity check comes in Epic 2
- Reference FR-001

**Prerequisites:** Story 1.4, 1.7

---

## Epic 2: Read Operations (Customer & Orders)

**Goal:** Implement read-only tools for customer and sales order data. After this epic, users can look up customers and check order status via MCP.

**User Value:** Sales and Customer Service can answer customer questions instantly without navigating AX.

**FRs Covered:** FR-002, FR-003  
**Architecture Sections:** 3.1, 2.2.3

---

### Story 2.1: AIF Client Setup

As a **developer**,  
I want an AIF client that can call AX 2012 standard services,  
So that I can implement read operations.

**Acceptance Criteria:**

**Given** the AIF client is configured with BaseUrl and credentials  
**When** I call a method like `GetCustomerAsync`  
**Then** a SOAP request is sent to the AIF endpoint

**And** Windows Authentication is used (Kerberos)  
**And** the response is parsed from SOAP XML  
**And** timeout is respected (default: 30s)  
**And** errors are wrapped in `AxException`

**Technical Notes:**
- Use `HttpClient` with `HttpClientHandler` for Windows Auth
- Build SOAP envelopes manually or use generated proxies
- Reference Architecture Section 3.1

**Prerequisites:** Epic 1 complete

---

### Story 2.2: Get Customer Tool - By Account

As a **Sales Representative**,  
I want to look up a customer by account number,  
So that I can quickly access their information during a call.

**Acceptance Criteria:**

**Given** I have MCP_Sales_Read role  
**When** I call `ax_get_customer` with `customer_account: "CUST-001"`  
**Then** I receive the customer data within 500ms (p95)

**And** the response includes:
```json
{
  "customer_account": "CUST-001",
  "name": "Müller GmbH",
  "currency": "EUR",
  "credit_limit": 100000.00,
  "credit_used": 45000.00,
  "payment_terms": "Net30",
  "price_group": "STANDARD"
}
```

**And** if customer not found, error code is `CUST_NOT_FOUND`  
**And** the operation is logged to audit

**Technical Notes:**
- Use AIF `CustCustomerService.find` operation
- Map AX fields to response schema
- Reference FR-002, Architecture Section 3.1

**Prerequisites:** Story 2.1

---

### Story 2.3: Get Customer Tool - Fuzzy Search

As a **Customer Service Rep**,  
I want to search for customers by name,  
So that I can find them even if I don't know the exact account number.

**Acceptance Criteria:**

**Given** I have MCP_Sales_Read role  
**When** I call `ax_get_customer` with `customer_name: "Müller"`  
**Then** I receive up to 5 matching customers

**And** each result includes a confidence score (0-100)  
**And** results are sorted by confidence descending  
**And** partial matches are included (e.g., "Müller" matches "Müller GmbH")

**And** the response format is:
```json
{
  "matches": [
    { "customer_account": "CUST-001", "name": "Müller GmbH", "confidence": 95 },
    { "customer_account": "CUST-042", "name": "Hans Müller AG", "confidence": 80 }
  ]
}
```

**Technical Notes:**
- Use AIF `CustCustomerService.find` with name criteria
- Implement fuzzy matching in MCP Server (Levenshtein or similar)
- Reference FR-002

**Prerequisites:** Story 2.2

---

### Story 2.4: Get Sales Order Tool - By Sales ID

As a **Customer Service Rep**,  
I want to look up an order by Sales ID,  
So that I can answer "where is my order" questions.

**Acceptance Criteria:**

**Given** I have MCP_Sales_Read role  
**When** I call `ax_get_salesorder` with `sales_id: "SO-2025-001234"`  
**Then** I receive the order data within 500ms (p95)

**And** the response includes:
```json
{
  "sales_id": "SO-2025-001234",
  "customer_account": "CUST-001",
  "customer_name": "Müller GmbH",
  "order_date": "2025-12-06",
  "requested_delivery": "2025-12-13",
  "status": "Open",
  "total_amount": 5000.00,
  "currency": "EUR"
}
```

**And** if `include_lines: true`, order lines are included  
**And** if order not found, error code is `ORDER_NOT_FOUND`

**Technical Notes:**
- Use AIF `SalesSalesOrderService.read` operation
- Reference FR-003, Architecture Section 3.1

**Prerequisites:** Story 2.1

---

### Story 2.5: Get Sales Order Tool - With Lines

As a **Customer Service Rep**,  
I want to see order line details,  
So that I can tell customers which items are reserved or shipped.

**Acceptance Criteria:**

**Given** I have MCP_Sales_Read role  
**When** I call `ax_get_salesorder` with `sales_id: "SO-2025-001234"` and `include_lines: true`  
**Then** the response includes order lines:

```json
{
  "sales_id": "SO-2025-001234",
  "lines": [
    {
      "line_num": 1,
      "item_id": "WIDGET-PRO",
      "item_name": "Widget Professional",
      "quantity": 50,
      "unit_price": 100.00,
      "line_amount": 5000.00,
      "reserved_qty": 30,
      "delivered_qty": 0,
      "status": "Partially Reserved"
    }
  ]
}
```

**And** line status is calculated from reserved/delivered quantities

**Technical Notes:**
- Parse SalesLine records from AIF response
- Calculate line status based on quantities
- Reference FR-003

**Prerequisites:** Story 2.4

---

### Story 2.6: Get Sales Order Tool - By Customer

As a **Sales Representative**,  
I want to see all orders for a customer,  
So that I can review their order history.

**Acceptance Criteria:**

**Given** I have MCP_Sales_Read role  
**When** I call `ax_get_salesorder` with `customer_account: "CUST-001"`  
**Then** I receive a list of orders for that customer

**And** results are paginated (default: 20 per page)  
**And** I can filter by `status_filter` (e.g., ["Open", "Confirmed"])  
**And** I can filter by `date_from` and `date_to`  
**And** results are sorted by order date descending

**Technical Notes:**
- Use AIF `SalesSalesOrderService.find` with customer criteria
- Implement pagination in MCP Server
- Reference FR-003

**Prerequisites:** Story 2.4

---

## Epic 3: Inventory & Pricing

**Goal:** Implement inventory availability and price simulation tools. After this epic, users can check stock and get price quotes without creating orders.

**User Value:** Sales can promise realistic delivery dates and answer pricing questions immediately.

**FRs Covered:** FR-004, FR-005  
**Architecture Sections:** 3.1, 3.2

---

### Story 3.1: Check Inventory Tool - Basic

As a **Sales Representative**,  
I want to check if an item is in stock,  
So that I can promise realistic delivery dates.

**Acceptance Criteria:**

**Given** I have MCP_Inventory_Read role  
**When** I call `ax_check_inventory` with `item_id: "WIDGET-PRO"`  
**Then** I receive inventory data within 500ms (p95)

**And** the response includes:
```json
{
  "item_id": "WIDGET-PRO",
  "item_name": "Widget Professional",
  "total_on_hand": 500,
  "available": 320,
  "reserved": 180,
  "on_order": 200
}
```

**And** `available = total_on_hand - reserved`  
**And** if item not found, error code is `ITEM_NOT_FOUND`

**Technical Notes:**
- Use AIF `InventInventSumService` or custom query
- Reference FR-004, Architecture Section 3.1

**Prerequisites:** Story 2.1

---

### Story 3.2: Check Inventory Tool - By Warehouse

As a **Sales Representative**,  
I want to see inventory breakdown by warehouse,  
So that I can ship from the nearest location.

**Acceptance Criteria:**

**Given** I have MCP_Inventory_Read role  
**When** I call `ax_check_inventory` with `item_id: "WIDGET-PRO"` and `include_reservations: true`  
**Then** the response includes warehouse breakdown:

```json
{
  "item_id": "WIDGET-PRO",
  "warehouses": [
    {
      "warehouse_id": "WH-MAIN",
      "on_hand": 400,
      "available": 250,
      "reserved": 150
    },
    {
      "warehouse_id": "WH-EAST",
      "on_hand": 100,
      "available": 70,
      "reserved": 30
    }
  ]
}
```

**And** I can filter by specific `warehouse` parameter

**Technical Notes:**
- Query InventSum with InventDim grouping
- Reference FR-004

**Prerequisites:** Story 3.1

---

### Story 3.3: Simulate Price Tool - Basic

As a **Sales Representative**,  
I want to get a price quote without creating an order,  
So that I can answer pricing questions immediately.

**Acceptance Criteria:**

**Given** I have MCP_Sales_Read role  
**When** I call `ax_simulate_price` with:
- `customer_account: "CUST-001"`
- `item_id: "WIDGET-PRO"`
- `quantity: 50`

**Then** I receive price data within 1 second (p95)

**And** the response includes:
```json
{
  "customer_account": "CUST-001",
  "item_id": "WIDGET-PRO",
  "quantity": 50,
  "unit": "PCS",
  "base_price": 120.00,
  "customer_discount_pct": 10.0,
  "quantity_discount_pct": 5.0,
  "final_unit_price": 102.60,
  "line_amount": 5130.00,
  "currency": "EUR",
  "price_source": "Trade Agreement",
  "valid_until": "2025-12-31"
}
```

**Technical Notes:**
- Use custom WCF service or AX price simulation API
- Apply trade agreements, customer discounts, quantity breaks
- Reference FR-005, Architecture Section 3.2

**Prerequisites:** Story 2.1

---

### Story 3.4: Simulate Price Tool - Date Override

As a **Sales Representative**,  
I want to simulate prices for a future date,  
So that I can quote prices for future orders.

**Acceptance Criteria:**

**Given** I have MCP_Sales_Read role  
**When** I call `ax_simulate_price` with `date: "2026-01-15"`  
**Then** the price is calculated using trade agreements valid on that date

**And** if no valid price exists for that date, error code is `NO_VALID_PRICE`  
**And** the response includes `valid_until` showing when the price expires

**Technical Notes:**
- Pass date parameter to AX price simulation
- Reference FR-005

**Prerequisites:** Story 3.3

---

## Epic 4: Order Creation & Validation

**Goal:** Implement the create sales order tool with full validation and idempotency. After this epic, users can create orders via MCP with confidence.

**User Value:** Sales can create orders via chat in 30 seconds instead of 5 minutes in AX.

**FRs Covered:** FR-006  
**Architecture Sections:** 3.2, 4.3

---

### Story 4.1: WCF Client Setup

As a **developer**,  
I want a WCF client that can call custom AX services,  
So that I can implement write operations.

**Acceptance Criteria:**

**Given** the WCF client is configured with BaseUrl and service account  
**When** I call a method like `CreateSalesOrderAsync`  
**Then** a SOAP request is sent to the custom WCF endpoint

**And** service account credentials are used (Windows Auth)  
**And** timeout is respected (default: 30s)  
**And** the circuit breaker wraps all calls  
**And** errors are wrapped in `AxException`

**Technical Notes:**
- Use `ChannelFactory<T>` with `BasicHttpBinding`
- Configure Windows credential type
- Reference Architecture Section 3.2

**Prerequisites:** Epic 1 complete

---

### Story 4.2: Idempotency Store

As a **developer**,  
I want an idempotency store that prevents duplicate orders,  
So that retries are safe.

**Acceptance Criteria:**

**Given** a write operation is called with `idempotency_key: "abc-123"`  
**When** the operation completes successfully  
**Then** the result is stored with the idempotency key

**And** if the same key is used again within 7 days  
**Then** the cached result is returned without executing the operation  
**And** the response includes `duplicate: true` flag

**And** if the key is used after 7 days, it's treated as a new request

**Technical Notes:**
- Use distributed cache (Redis or SQL)
- Store serialized response with TTL
- Reference Architecture Section 4.3

**Prerequisites:** Story 1.3

---

### Story 4.3: Create Sales Order - Input Validation

As a **developer**,  
I want input validation for create sales order,  
So that invalid requests are rejected before calling AX.

**Acceptance Criteria:**

**Given** I call `ax_create_salesorder`  
**When** input validation runs  
**Then** the following rules are enforced:

| Field | Rule | Error Code |
|-------|------|------------|
| `customer_account` | Required, non-empty | `INVALID_INPUT` |
| `lines` | Required, at least 1 line | `INVALID_INPUT` |
| `lines[].item_id` | Required, non-empty | `INVALID_INPUT` |
| `lines[].quantity` | Required, > 0 | `INVALID_QTY` |
| `idempotency_key` | Required, UUID format | `INVALID_INPUT` |

**And** validation errors include field name and message  
**And** all validation errors are returned together (not one at a time)

**Technical Notes:**
- Use FluentValidation
- Reference FR-006 Validation Rules

**Prerequisites:** Story 1.7

---

### Story 4.4: Create Sales Order - AX Validation

As a **developer**,  
I want AX reference validation before creating orders,  
So that orders don't fail due to invalid references.

**Acceptance Criteria:**

**Given** input validation passes  
**When** AX validation runs  
**Then** the following checks are performed:

| Check | Error Code |
|-------|------------|
| Customer exists | `CUST_NOT_FOUND` |
| Customer not blocked | `CUST_BLOCKED` |
| All items exist | `ITEM_NOT_FOUND` |
| Items not blocked for sales | `ITEM_BLOCKED` |
| Credit limit not exceeded | `CREDIT_EXCEEDED` |

**And** validation stops at first error (fail-fast)  
**And** error message includes the specific entity (e.g., "Item WIDGET-X not found")

**Technical Notes:**
- Call AIF services to validate references
- Calculate order total for credit check
- Reference FR-006 Validation Rules

**Prerequisites:** Story 4.3, Story 2.2, Story 3.3

---

### Story 4.5: Create Sales Order - Order Creation

As a **Sales Representative**,  
I want to create a sales order via MCP,  
So that I can serve customers faster.

**Acceptance Criteria:**

**Given** I have MCP_Sales_Write role  
**And** all validation passes  
**When** I call `ax_create_salesorder` with valid data  
**Then** the order is created in AX within 2 seconds (p95)

**And** the response includes:
```json
{
  "success": true,
  "sales_id": "SO-2025-001234",
  "customer_account": "CUST-001",
  "order_date": "2025-12-06",
  "total_amount": 5130.00,
  "currency": "EUR",
  "lines_created": 1,
  "warnings": [],
  "audit_id": "AUD-2025-12-06-001234"
}
```

**And** the order appears in AX immediately  
**And** the result is stored in idempotency cache

**Technical Notes:**
- Call custom WCF `CreateSalesOrder` operation
- Reference FR-006, Architecture Section 3.2

**Prerequisites:** Story 4.1, 4.2, 4.4

---

### Story 4.6: Create Sales Order - Error Handling

As a **developer**,  
I want proper error handling for order creation,  
So that users get clear feedback when something goes wrong.

**Acceptance Criteria:**

**Given** order creation fails in AX  
**When** the error is returned  
**Then** the error is mapped to a user-friendly message:

| AX Error | MCP Error Code | User Message |
|----------|----------------|--------------|
| Timeout | `AX_TIMEOUT` | "System temporarily unavailable" |
| Unknown error | `AX_ERROR` | "System error" |
| Validation error | `AX_VALIDATION` | Specific message from AX |

**And** the error is logged with full details  
**And** the audit entry shows `success: false`  
**And** the idempotency key is NOT stored (allow retry)

**Technical Notes:**
- Parse AX error responses
- Map to standard error codes
- Reference PRD Appendix A

**Prerequisites:** Story 4.5

---

### Story 4.7: Create Sales Order - Audit Trail

As an **IT Administrator**,  
I want all order creations logged,  
So that I can track who created what and when.

**Acceptance Criteria:**

**Given** an order is created (success or failure)  
**When** the operation completes  
**Then** an audit entry is created with:

```json
{
  "timestamp": "2025-12-06T14:30:00Z",
  "user_id": "CORP\\jsmith",
  "tool": "ax_create_salesorder",
  "input": { "customer_account": "CUST-001", "...": "..." },
  "output": { "sales_id": "SO-2025-001234", "...": "..." },
  "success": true,
  "duration_ms": 1234,
  "correlation_id": "abc-123-def"
}
```

**And** audit entries are stored in the database  
**And** sensitive data (if any) is masked in logs

**Technical Notes:**
- Use database audit service for writes
- Reference NFR-003, Architecture Section 5

**Prerequisites:** Story 4.5

---

## Epic 5: Security & Audit

**Goal:** Implement authentication, authorization, and audit logging. After this epic, the system is secure and compliant.

**User Value:** IT and Compliance can trust the system is secure and auditable.

**FRs Covered:** NFR-003, US-006, US-007  
**Architecture Sections:** 5, ADR-002

---

### Story 5.1: Windows Authentication

As a **developer**,  
I want Windows Authentication for MCP clients,  
So that users are identified by their AD account.

**Acceptance Criteria:**

**Given** a user connects to the MCP Server  
**When** authentication is performed  
**Then** the user's Windows identity is captured

**And** the identity is available in `ToolContext.UserId`  
**And** anonymous requests are rejected with `UNAUTHORIZED`  
**And** authentication failures are logged

**Technical Notes:**
- Use Windows Authentication middleware
- Extract identity from Kerberos token
- Reference ADR-002

**Prerequisites:** Epic 1 complete

---

### Story 5.2: Role-Based Authorization

As a **developer**,  
I want role-based authorization for tools,  
So that users can only access what they're allowed to.

**Acceptance Criteria:**

**Given** a user calls a tool  
**When** authorization is checked  
**Then** the user's AD groups are mapped to MCP roles:

| AD Group | MCP Role |
|----------|----------|
| CORP\MCP-Users-Read | MCP_Read |
| CORP\MCP-Users-Write | MCP_Write |
| CORP\MCP-Admins | MCP_Admin |

**And** each tool requires specific roles:

| Tool | Required Role |
|------|---------------|
| `ax_health_check` | MCP_Read |
| `ax_get_customer` | MCP_Read |
| `ax_get_salesorder` | MCP_Read |
| `ax_check_inventory` | MCP_Read |
| `ax_simulate_price` | MCP_Read |
| `ax_create_salesorder` | MCP_Write |

**And** unauthorized access returns `FORBIDDEN`

**Technical Notes:**
- Use `[Authorize(Roles = "...")]` attribute or equivalent
- Reference ADR-002, PRD Appendix B

**Prerequisites:** Story 5.1

---

### Story 5.3: Database Audit Service

As an **IT Administrator**,  
I want write operations logged to a database,  
So that I can query and analyze audit data.

**Acceptance Criteria:**

**Given** a write operation completes  
**When** the audit entry is created  
**Then** it is stored in the `MCP_Audit` database

**And** the schema includes:
- `Id` (GUID)
- `Timestamp` (DateTime)
- `UserId` (string)
- `ToolName` (string)
- `Input` (JSON)
- `Output` (JSON)
- `Success` (bool)
- `DurationMs` (int)
- `CorrelationId` (string)
- `ErrorMessage` (string, nullable)

**And** data is retained for 90 days  
**And** old data is automatically purged

**Technical Notes:**
- Use Entity Framework Core
- Implement `IAuditService`
- Reference Architecture Section 5

**Prerequisites:** Story 1.3

---

### Story 5.4: File Audit Service

As an **IT Administrator**,  
I want read operations logged to files,  
So that I have a lightweight audit trail for reads.

**Acceptance Criteria:**

**Given** a read operation completes  
**When** the audit entry is created  
**Then** it is written to a log file

**And** log files are rotated daily  
**And** files are named `mcp-audit-YYYY-MM-DD.json`  
**And** each line is a JSON object (JSONL format)  
**And** files are retained for 30 days

**Technical Notes:**
- Use Serilog file sink
- Reference Architecture Section 5

**Prerequisites:** Story 1.3

---

### Story 5.5: Audit Query Tool

As an **IT Administrator**,  
I want to query the audit log via MCP,  
So that I can troubleshoot issues without database access.

**Acceptance Criteria:**

**Given** I have MCP_Admin role  
**When** I call `ax_query_audit` with filters  
**Then** I receive matching audit entries

**And** I can filter by:
- `user_id`
- `tool_name`
- `date_from` / `date_to`
- `success` (true/false)
- `correlation_id`

**And** results are paginated (default: 50 per page)  
**And** results are sorted by timestamp descending

**Technical Notes:**
- Query database audit table
- Reference US-007

**Prerequisites:** Story 5.3

---

## Epic 6: Resilience & Monitoring

**Goal:** Implement health monitoring, metrics, and operational tooling. After this epic, the system is production-ready.

**User Value:** IT can monitor system health and proactively identify issues.

**FRs Covered:** NFR-001, NFR-002, NFR-004, US-006  
**Architecture Sections:** 6, 8

---

### Story 6.1: Full Health Check

As an **IT Administrator**,  
I want a comprehensive health check,  
So that I can verify all components are working.

**Acceptance Criteria:**

**Given** I have MCP_Admin role  
**When** I call `ax_health_check` with `include_details: true`  
**Then** I receive detailed component status:

```json
{
  "status": "healthy",
  "aos_connected": true,
  "response_time_ms": 45,
  "timestamp": "2025-12-06T14:00:00Z",
  "details": {
    "database": "connected",
    "business_connector": "connected",
    "aif_services": "connected",
    "wcf_services": "connected",
    "rate_limiter": "enabled",
    "circuit_breaker": "closed"
  }
}
```

**And** if any component is unhealthy, overall status is "degraded" or "unhealthy"

**Technical Notes:**
- Use Business Connector for AX connectivity check
- Reference FR-001, US-006

**Prerequisites:** Story 1.8, Epic 2 complete

---

### Story 6.2: HTTP Health Endpoints

As a **DevOps Engineer**,  
I want HTTP health endpoints,  
So that load balancers can check server health.

**Acceptance Criteria:**

**Given** the MCP Server is running  
**When** I call HTTP endpoints  
**Then** I receive appropriate responses:

| Endpoint | Purpose | Response |
|----------|---------|----------|
| `/health/live` | Server is running | 200 OK |
| `/health/ready` | Server can serve requests | 200 OK or 503 |
| `/health` | Detailed status | JSON with component status |

**And** `/health/ready` returns 503 if AX is unreachable

**Technical Notes:**
- Use ASP.NET Core health checks
- Reference Architecture Section 6.2

**Prerequisites:** Story 6.1

---

### Story 6.3: Prometheus Metrics

As a **DevOps Engineer**,  
I want Prometheus metrics exposed,  
So that I can monitor the system in Grafana.

**Acceptance Criteria:**

**Given** the MCP Server is running  
**When** I call `/metrics`  
**Then** I receive Prometheus-format metrics:

| Metric | Type | Labels |
|--------|------|--------|
| `mcp_tool_calls_total` | Counter | tool, status |
| `mcp_tool_call_duration_seconds` | Histogram | tool |
| `mcp_circuit_breaker_state` | Gauge | - |
| `mcp_rate_limit_hits_total` | Counter | user |
| `mcp_active_connections` | Gauge | - |

**Technical Notes:**
- Use prometheus-net library
- Reference Architecture Section 8.1

**Prerequisites:** Story 1.3

---

### Story 6.4: Structured Logging

As a **DevOps Engineer**,  
I want structured JSON logs,  
So that I can analyze logs in Seq/ELK.

**Acceptance Criteria:**

**Given** the MCP Server is running  
**When** log events occur  
**Then** logs are written in JSON format

**And** each log entry includes:
- `Timestamp`
- `Level`
- `Message`
- `CorrelationId`
- `UserId` (if available)
- `ToolName` (if applicable)
- `MachineName`
- `Environment`

**And** logs are written to console and file  
**And** log files are rotated daily

**Technical Notes:**
- Use Serilog with JSON formatter
- Reference Architecture Section 8.2

**Prerequisites:** Story 1.2

---

## FR Coverage Matrix

| FR/NFR | Description | Stories |
|--------|-------------|---------|
| **FR-001** | Health Check | 1.8, 6.1, 6.2 |
| **FR-002** | Get Customer | 2.1, 2.2, 2.3 |
| **FR-003** | Get Sales Order | 2.4, 2.5, 2.6 |
| **FR-004** | Check Inventory | 3.1, 3.2 |
| **FR-005** | Simulate Price | 3.3, 3.4 |
| **FR-006** | Create Sales Order | 4.1-4.7 |
| **NFR-001** | Performance | 1.3, 6.2, 6.3 |
| **NFR-002** | Availability | 6.1, 6.2, 6.4 |
| **NFR-003** | Security | 5.1-5.5 |
| **NFR-004** | Resilience | 1.5, 1.6, 4.2 |
| **US-001** | Quick Order Entry | 4.5 |
| **US-002** | Customer Lookup | 2.2, 2.3 |
| **US-003** | Order Status Check | 2.4, 2.5 |
| **US-004** | Price Quote | 3.3, 3.4 |
| **US-005** | Inventory Check | 3.1, 3.2 |
| **US-006** | Health Monitoring | 6.1, 6.2 |
| **US-007** | Audit Trail | 5.3, 5.4, 5.5 |

---

## Summary

| Metric | Value |
|--------|-------|
| **Total Epics** | 8 |
| **Total Stories** | 46 (34 + 12 new) |
| **FRs Covered** | 6/6 (100%) |
| **NFRs Covered** | 4/4 (100%) |
| **User Stories Covered** | 7/7 (100%) |

### Epic Dependency Graph

```
Epic 1: Foundation
    │
    ├──> Epic 2: Read Operations
    │        │
    │        ├──> Epic 3: Inventory & Pricing
    │        │
    │        └──> Epic 4: Order Creation
    │
    ├──> Epic 5: Security & Audit
    │
    └──> Epic 6: Resilience & Monitoring
    │
    ├──> Epic 7: Batch Operations & Webhooks (NEW)
    │
    └──> Epic 8: Zero-Downtime & Self-Healing (NEW)
```

### Recommended Implementation Order

1. **Epic 1** - Foundation (required for all others)
2. **Epic 5** - Security (parallel with Epic 2)
3. **Epic 2** - Read Operations
4. **Epic 3** - Inventory & Pricing
5. **Epic 4** - Order Creation
6. **Epic 6** - Resilience & Monitoring
7. **Epic 7** - Batch Operations & Webhooks (Phase 2)
8. **Epic 8** - Zero-Downtime & Self-Healing (Phase 3)

---

_For implementation: Use the `dev-story` workflow to generate individual story implementation plans from this epic breakdown._

_This document is ready for Sprint Planning and Azure DevOps import._
