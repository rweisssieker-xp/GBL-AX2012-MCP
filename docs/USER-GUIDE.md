# GBL-AX2012-MCP User Guide

## Overview

GBL-AX2012-MCP is a Model Context Protocol (MCP) server that enables AI assistants and automation tools to interact with Microsoft Dynamics AX 2012 R3. It provides a complete Order-to-Cash (O2C) automation capability.

---

## Quick Start

### For Claude Desktop Users

1. Add to your `claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "ax2012": {
      "command": "dotnet",
      "args": ["run", "--project", "C:\\path\\to\\GBL.AX2012.MCP.Server"]
    }
  }
}
```

2. Restart Claude Desktop
3. Start using natural language commands:

```
"Show me customer CUST-001"
"Create a sales order for Müller GmbH, 50 units of Widget Pro"
"Check inventory for item ITEM-100"
```

---

## Available Tools

### Phase 1: Order Capture

#### `ax_health_check`
Check server and AX connectivity.

```json
{
  "includeDetails": true
}
```

#### `ax_get_customer`
Get customer by account or search by name.

```json
// By account
{ "customerAccount": "CUST-001" }

// By name (fuzzy search)
{ "customerName": "Müller" }
```

#### `ax_get_salesorder`
Get sales order details.

```json
// By order ID
{ "salesId": "SO-2024-001", "includeLines": true }

// By customer
{ "customerAccount": "CUST-001", "statusFilter": ["Open"] }
```

#### `ax_check_inventory`
Check item availability.

```json
{
  "itemId": "ITEM-100",
  "warehouse": "WH-MAIN",
  "includeWarehouses": true
}
```

#### `ax_simulate_price`
Simulate pricing without creating an order.

```json
{
  "customerAccount": "CUST-001",
  "itemId": "ITEM-100",
  "quantity": 50,
  "date": "2024-12-15"
}
```

#### `ax_create_salesorder`
Create a new sales order.

```json
{
  "customerAccount": "CUST-001",
  "requestedDelivery": "2024-12-20",
  "customerRef": "PO-12345",
  "lines": [
    { "itemId": "ITEM-100", "quantity": 50 },
    { "itemId": "ITEM-200", "quantity": 25, "unitPrice": 99.99 }
  ],
  "idempotencyKey": "unique-uuid-here"
}
```

**Important:** Always provide a unique `idempotencyKey` to prevent duplicate orders.

---

### Phase 2: Fulfillment

#### `ax_reserve_salesline`
Reserve inventory for an order line.

```json
{
  "salesId": "SO-2024-001",
  "lineNum": 1,
  "quantity": 50,
  "warehouse": "WH-MAIN"
}
```

#### `ax_post_shipment`
Post a shipment/packing slip.

```json
{
  "salesId": "SO-2024-001",
  "trackingNumber": "DHL-123456",
  "carrier": "DHL"
}
```

---

### Phase 3: Invoice & Dunning

#### `ax_create_invoice`
Create and post an invoice.

```json
{
  "salesId": "SO-2024-001",
  "postImmediately": true
}
```

#### `ax_get_customer_aging`
Get AR aging and open invoices.

```json
{
  "customerAccount": "CUST-001",
  "includeOpenInvoices": true
}
```

---

### Phase 4: Payment & Close

#### `ax_post_payment`
Post a customer payment.

```json
{
  "customerAccount": "CUST-001",
  "amount": 5000.00,
  "currency": "EUR",
  "paymentReference": "BANK-REF-123",
  "invoicesToSettle": ["INV-2024-001"]
}
```

#### `ax_settle_invoice`
Settle an invoice against a payment.

```json
{
  "invoiceId": "INV-2024-001",
  "paymentId": "PAY-2024-001"
}
```

#### `ax_close_salesorder`
Close a completed order.

```json
{
  "salesId": "SO-2024-001",
  "reason": "Completed"
}
```

---

### Approval Workflow

#### `ax_request_approval`
Request approval for high-value operations.

```json
{
  "type": "high_value_order",
  "description": "Order for CUST-001, total €75,000",
  "amount": 75000,
  "currency": "EUR"
}
```

#### `ax_get_approval_status`
Check approval status.

```json
{
  "approvalId": "approval-uuid"
}
```

---

## HTTP API

The server also exposes an HTTP API on port 8080.

### Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/health` | GET | Health check |
| `/tools` | GET | List available tools |
| `/tools/call` | POST | Execute a tool |
| `/mcp` | POST | MCP JSON-RPC endpoint |

### Example: Call a Tool via HTTP

```bash
curl -X POST http://localhost:8080/tools/call \
  -H "Content-Type: application/json" \
  -d '{
    "tool": "ax_get_customer",
    "arguments": {
      "customerAccount": "CUST-001"
    }
  }'
```

---

## n8n Integration

Import the workflow templates from `n8n-workflows/`:

1. **order-from-email.json** - Automatically create orders from emails
2. **order-fulfillment.json** - Reserve, ship, and invoice orders
3. **dunning-automation.json** - Automated payment reminders
4. **payment-processing.json** - Process bank statement payments

---

## Monitoring

### Prometheus Metrics

Available at `http://localhost:9090/metrics`:

- `mcp_tool_calls_total` - Total tool calls by tool and status
- `mcp_tool_call_duration_seconds` - Tool execution time
- `mcp_circuit_breaker_state` - Circuit breaker status
- `mcp_rate_limit_hits_total` - Rate limit violations
- `mcp_uptime_seconds` - Server uptime

### Grafana Dashboard

Import `monitoring/grafana/dashboards/mcp-server.json` for a pre-built dashboard.

---

## Security

### Authentication
- Windows Authentication (NTLM/Kerberos)
- Automatic user identification

### Authorization
- **MCP_Read**: Read-only tools
- **MCP_Write**: Create/update operations
- **MCP_Admin**: Audit queries, admin functions

### Rate Limiting
- 100 requests per minute per user
- Configurable in `appsettings.json`

### Approval Workflow
- Orders above €50,000 require approval
- Configurable threshold

---

## Troubleshooting

### Common Issues

**"Circuit breaker is open"**
- AX server may be unavailable
- Wait 60 seconds and retry
- Check AX connectivity

**"Rate limit exceeded"**
- Too many requests in short time
- Wait 1 minute and retry

**"Customer not found"**
- Check customer account spelling
- Use `customerName` for fuzzy search

**"Validation error"**
- Check required fields
- Ensure `idempotencyKey` is a valid UUID

### Logs

Check `logs/mcp-*.log` for detailed error information.

---

## Support

For issues, contact: IT-Support@company.com
