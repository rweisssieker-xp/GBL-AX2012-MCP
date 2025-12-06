# GBL-AX2012-MCP API Reference

## HTTP API

Base URL: `http://localhost:8080`

---

## Endpoints

### Health Check

```http
GET /health
```

**Response:**
```json
{
  "status": "healthy",
  "timestamp": "2024-12-06T14:30:00Z",
  "serverVersion": "1.1.0",
  "aosConnected": true,
  "responseTimeMs": 45,
  "details": {
    "server": "running",
    "circuit_breaker": "closed",
    "business_connector": "connected"
  }
}
```

---

### List Tools

```http
GET /tools
```

**Response:**
```json
{
  "tools": [
    { "name": "ax_health_check", "description": "Check server and AX connectivity" },
    { "name": "ax_get_customer", "description": "Get customer by account or name" },
    ...
  ]
}
```

---

### Call Tool

```http
POST /tools/call
Content-Type: application/json
```

**Request:**
```json
{
  "tool": "ax_get_customer",
  "arguments": {
    "customerAccount": "CUST-001"
  }
}
```

**Response (Success):**
```json
{
  "success": true,
  "data": {
    "customerAccount": "CUST-001",
    "name": "Müller GmbH",
    "currency": "EUR",
    "creditLimit": 100000,
    "creditUsed": 25000,
    "creditAvailable": 75000,
    "paymentTerms": "Net30",
    "priceGroup": "RETAIL",
    "blocked": false
  },
  "durationMs": 123
}
```

**Response (Error):**
```json
{
  "success": false,
  "errorCode": "CUST_NOT_FOUND",
  "errorMessage": "Customer INVALID not found",
  "durationMs": 45
}
```

---

### MCP JSON-RPC

```http
POST /mcp
Content-Type: application/json
```

Supports standard MCP JSON-RPC protocol.

**Initialize:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "initialize",
  "params": {
    "protocolVersion": "2024-11-05",
    "capabilities": {},
    "clientInfo": { "name": "test", "version": "1.0" }
  }
}
```

**List Tools:**
```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "method": "tools/list"
}
```

**Call Tool:**
```json
{
  "jsonrpc": "2.0",
  "id": 3,
  "method": "tools/call",
  "params": {
    "name": "ax_get_customer",
    "arguments": { "customerAccount": "CUST-001" }
  }
}
```

---

## Tool Schemas

### ax_health_check

**Input:**
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| includeDetails | boolean | No | Include component status details |

**Output:**
| Field | Type | Description |
|-------|------|-------------|
| status | string | "healthy", "degraded", "unhealthy" |
| timestamp | datetime | Check timestamp |
| serverVersion | string | Server version |
| aosConnected | boolean | AX AOS connectivity |
| responseTimeMs | number | Response time in ms |
| details | object | Component status details |

---

### ax_get_customer

**Input:**
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| customerAccount | string | No* | Customer account number |
| customerName | string | No* | Customer name (fuzzy search) |
| includeAddresses | boolean | No | Include addresses |
| includeContacts | boolean | No | Include contacts |

*Either customerAccount or customerName required

**Output (by account):**
| Field | Type | Description |
|-------|------|-------------|
| customerAccount | string | Account number |
| name | string | Customer name |
| currency | string | Currency code |
| creditLimit | decimal | Credit limit |
| creditUsed | decimal | Credit used |
| creditAvailable | decimal | Available credit |
| paymentTerms | string | Payment terms |
| priceGroup | string | Price group |
| blocked | boolean | Blocked status |

**Output (by name):**
| Field | Type | Description |
|-------|------|-------------|
| matches | array | Matching customers |
| matches[].customerAccount | string | Account number |
| matches[].name | string | Customer name |
| matches[].confidence | number | Match confidence (0-100) |

---

### ax_create_salesorder

**Input:**
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| customerAccount | string | Yes | Customer account |
| requestedDelivery | date | No | Requested delivery date |
| customerRef | string | No | Customer reference |
| lines | array | Yes | Order lines |
| lines[].itemId | string | Yes | Item ID |
| lines[].quantity | decimal | Yes | Quantity |
| lines[].unitPrice | decimal | No | Override unit price |
| lines[].warehouse | string | No | Warehouse |
| idempotencyKey | string | Yes | Unique UUID |

**Output:**
| Field | Type | Description |
|-------|------|-------------|
| success | boolean | Creation success |
| salesId | string | Created sales order ID |
| customerAccount | string | Customer account |
| orderDate | date | Order date |
| totalAmount | decimal | Total amount |
| currency | string | Currency |
| linesCreated | number | Number of lines |
| warnings | array | Warning messages |
| auditId | string | Audit trail ID |
| duplicate | boolean | True if idempotent duplicate |

---

## Error Codes

| Code | Description |
|------|-------------|
| VALIDATION_ERROR | Input validation failed |
| CUST_NOT_FOUND | Customer not found |
| CUST_BLOCKED | Customer is blocked |
| ITEM_NOT_FOUND | Item not found |
| ITEM_BLOCKED | Item blocked for sales |
| ORDER_NOT_FOUND | Sales order not found |
| LINE_NOT_FOUND | Order line not found |
| CREDIT_EXCEEDED | Credit limit exceeded |
| NOTHING_TO_SHIP | No lines to ship |
| NOTHING_TO_INVOICE | No lines to invoice |
| NO_VALID_PRICE | No valid price found |
| AIF_ERROR | AX AIF service error |
| WCF_ERROR | AX WCF service error |
| CIRCUIT_OPEN | Circuit breaker is open |
| RATE_LIMIT | Rate limit exceeded |
| UNAUTHORIZED | Authentication failed |
| FORBIDDEN | Authorization failed |

---

## Rate Limits

- **Default:** 100 requests per minute per user
- **Response Header:** `X-RateLimit-Remaining`
- **HTTP Status:** 429 when exceeded

---

## Prometheus Metrics

Endpoint: `http://localhost:9090/metrics`

| Metric | Type | Labels | Description |
|--------|------|--------|-------------|
| mcp_tool_calls_total | Counter | tool, status | Total tool calls |
| mcp_tool_call_duration_seconds | Histogram | tool | Call duration |
| mcp_circuit_breaker_state | Gauge | - | 0=closed, 1=open, 2=half-open |
| mcp_rate_limit_hits_total | Counter | user | Rate limit violations |
| mcp_ax_calls_total | Counter | service, operation, status | AX service calls |
| mcp_ax_call_duration_seconds | Histogram | service, operation | AX call duration |
| mcp_uptime_seconds | Gauge | - | Server uptime |
| mcp_errors_total | Counter | error_code, tool | Error count |
