# Batch Operations & Webhook Notifications

**Version:** 1.0  
**Date:** 2025-12-06  
**Status:** Implemented

---

## Overview

Epic 7 introduces two powerful features for enterprise-scale operations:

1. **Batch Operations** - Process multiple operations in a single call
2. **Webhook Notifications** - Real-time event-driven integrations

---

## Batch Operations

### Tool: `ax_batch_operations`

Execute multiple MCP operations in a single batch call. Perfect for:
- EDI file processing
- Bulk imports
- Multi-step workflows
- Performance optimization

### Usage

```json
{
  "tool": "ax_batch_operations",
  "arguments": {
    "requests": [
      {
        "tool": "ax_create_salesorder",
        "arguments": {
          "customer_account": "CUST-001",
          "lines": [...],
          "idempotency_key": "uuid-1"
        }
      },
      {
        "tool": "ax_create_salesorder",
        "arguments": {
          "customer_account": "CUST-002",
          "lines": [...],
          "idempotency_key": "uuid-2"
        }
      },
      {
        "tool": "ax_check_inventory",
        "arguments": {
          "item_id": "ITEM-100"
        }
      }
    ],
    "stop_on_error": false,
    "max_parallel": 5
  }
}
```

### Response

```json
{
  "success": true,
  "data": {
    "total": 3,
    "successful": 2,
    "failed": 1,
    "results": [
      {
        "index": 0,
        "success": true,
        "output": {
          "sales_id": "SO-2025-001234",
          "customer_account": "CUST-001",
          ...
        },
        "duration_ms": 1234
      },
      {
        "index": 1,
        "success": true,
        "output": {...},
        "duration_ms": 1456
      },
      {
        "index": 2,
        "success": false,
        "error_code": "ITEM_NOT_FOUND",
        "error_message": "Item ITEM-100 not found",
        "duration_ms": 234
      }
    ],
    "duration_ms": 2345
  }
}
```

### Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `requests` | array | Yes | List of operations to execute |
| `requests[].tool` | string | Yes | Tool name (e.g., "ax_create_salesorder") |
| `requests[].arguments` | object | Yes | Tool-specific arguments |
| `stop_on_error` | boolean | No | Stop processing on first error (default: false) |
| `max_parallel` | integer | No | Max parallel operations (1-10, default: 5) |

### Features

- **Parallel Processing**: Execute up to 10 operations simultaneously
- **Error Handling**: Continue processing even if some operations fail
- **Individual Audit**: Each operation gets its own audit entry
- **Idempotency**: Each operation respects its own idempotency key
- **Rate Limiting**: Batch counts as 1 request for rate limiting

### Limitations

- Maximum 100 requests per batch
- All operations must be from same user context
- Circuit breaker applies to entire batch

---

## Webhook Notifications

### Overview

Subscribe to MCP events and receive real-time HTTP POST notifications when events occur.

### Supported Events

| Event Type | Description | Triggered By |
|------------|-------------|--------------|
| `salesorder.created` | New sales order created | `ax_create_salesorder` |
| `salesorder.updated` | Sales order updated | `ax_update_salesorder` |
| `payment.posted` | Payment posted | `ax_post_payment` |
| `invoice.created` | Invoice created | `ax_create_invoice` |
| `inventory.low_stock` | Inventory below threshold | `ax_check_inventory` |

### Tool: `ax_subscribe_webhook`

Subscribe to events.

```json
{
  "tool": "ax_subscribe_webhook",
  "arguments": {
    "event_type": "salesorder.created",
    "webhook_url": "https://n8n.example.com/webhook/order",
    "secret": "webhook-secret-key",
    "filters": {
      "customer_account": "CUST-001",
      "min_amount": 1000
    },
    "retry_policy": {
      "max_retries": 3,
      "backoff_ms": 1000,
      "exponential_backoff": true
    }
  }
}
```

### Response

```json
{
  "success": true,
  "data": {
    "subscription_id": "550e8400-e29b-41d4-a716-446655440000",
    "event_type": "salesorder.created",
    "webhook_url": "https://n8n.example.com/webhook/order",
    "created_at": "2025-12-06T14:30:00Z"
  }
}
```

### Webhook Payload

When an event occurs, your webhook URL receives:

```json
{
  "event": "salesorder.created",
  "timestamp": "2025-12-06T14:30:00Z",
  "data": {
    "sales_id": "SO-2025-001234",
    "customer_account": "CUST-001",
    "total_amount": 5000.00,
    "created_at": "2025-12-06T14:30:00Z",
    "user_id": "CORP\\jsmith"
  }
}
```

### Security

Webhooks include HMAC-SHA256 signature in `X-Webhook-Signature` header:

```
X-Webhook-Signature: a1b2c3d4e5f6...
```

Verify signature:
```csharp
using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
var signature = Convert.ToHexString(hash).ToLowerInvariant();
```

### Retry Policy

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `max_retries` | integer | 3 | Maximum retry attempts (0-10) |
| `backoff_ms` | integer | 1000 | Initial backoff in milliseconds |
| `exponential_backoff` | boolean | true | Use exponential backoff |

Retry schedule (exponential):
- Retry 1: after 1 second
- Retry 2: after 2 seconds
- Retry 3: after 4 seconds

### Tool: `ax_list_webhooks`

List all webhook subscriptions.

```json
{
  "tool": "ax_list_webhooks",
  "arguments": {
    "event_type": "salesorder.created"
  }
}
```

### Tool: `ax_unsubscribe_webhook`

Unsubscribe from a webhook.

```json
{
  "tool": "ax_unsubscribe_webhook",
  "arguments": {
    "subscription_id": "550e8400-e29b-41d4-a716-446655440000"
  }
}
```

### Configuration

Add to `appsettings.json`:

```json
{
  "Webhooks": {
    "MaxConcurrentDeliveries": 10,
    "DeliveryTimeoutSeconds": 30
  }
}
```

---

## ROI Metrics

### Tool: `ax_get_roi_metrics`

Get ROI metrics for MCP operations.

```json
{
  "tool": "ax_get_roi_metrics",
  "arguments": {
    "date_from": "2025-12-01",
    "date_to": "2025-12-06",
    "group_by": "tool"
  }
}
```

### Response

```json
{
  "success": true,
  "data": {
    "total_operations": 5000,
    "total_time_saved_hours": 250,
    "total_cost_saved_eur": 12500,
    "by_tool": [
      {
        "tool": "ax_create_salesorder",
        "operations": 2000,
        "avg_time_saved_sec": 270,
        "total_time_saved_hours": 150,
        "cost_saved_eur": 7500
      }
    ]
  }
}
```

### Baseline Times

| Tool | Manual Time (seconds) |
|------|----------------------|
| `ax_create_salesorder` | 300 (5 min) |
| `ax_get_customer` | 30 |
| `ax_get_salesorder` | 45 |
| `ax_check_inventory` | 60 |
| `ax_simulate_price` | 120 (2 min) |
| `ax_update_salesorder` | 180 (3 min) |
| `ax_create_invoice` | 240 (4 min) |
| `ax_post_payment` | 180 (3 min) |

---

## Examples

### Example 1: Batch Order Import

Process 50 orders from EDI file:

```json
{
  "tool": "ax_batch_operations",
  "arguments": {
    "requests": [
      // ... 50 order creation requests
    ],
    "max_parallel": 10,
    "stop_on_error": false
  }
}
```

### Example 2: Real-Time Order Notifications

Subscribe to order events for n8n workflow:

```json
{
  "tool": "ax_subscribe_webhook",
  "arguments": {
    "event_type": "salesorder.created",
    "webhook_url": "https://n8n.example.com/webhook/order-created",
    "secret": "my-secret-key"
  }
}
```

### Example 3: ROI Dashboard

Get monthly ROI metrics:

```json
{
  "tool": "ax_get_roi_metrics",
  "arguments": {
    "date_from": "2025-11-01",
    "date_to": "2025-11-30",
    "group_by": "tool"
  }
}
```

---

## Performance

### Batch Operations

- **Throughput**: 100 operations/batch
- **Latency**: <10 seconds for 100 operations
- **Parallelism**: Up to 10 concurrent operations

### Webhooks

- **Delivery Time**: <500ms (p95)
- **Success Rate**: >99%
- **Concurrent Deliveries**: 10 (configurable)

---

## Security

### Batch Operations

- Requires `MCP_Write` role
- Each operation respects its own authorization
- All operations audited individually

### Webhooks

- Requires `MCP_Admin` role
- HMAC-SHA256 signature verification
- HTTPS recommended for webhook URLs

---

## Troubleshooting

### Batch Operations Fail

**Problem**: Some operations in batch fail

**Solution**: 
- Check individual error messages in `results[]`
- Verify all tools exist and are accessible
- Check rate limits and circuit breaker status

### Webhook Not Delivered

**Problem**: Webhook not received

**Solution**:
1. Check subscription status: `ax_list_webhooks`
2. Verify webhook URL is accessible
3. Check webhook service logs
4. Review retry policy settings

### ROI Metrics Inaccurate

**Problem**: Metrics don't match expectations

**Solution**:
- Verify date range
- Check audit log completeness
- Review baseline time assumptions

---

## Best Practices

1. **Batch Operations**:
   - Use for bulk operations (>10 items)
   - Set `max_parallel` based on AX capacity
   - Use `stop_on_error: true` for critical workflows

2. **Webhooks**:
   - Always use HTTPS
   - Verify HMAC signatures
   - Implement idempotency in webhook handler
   - Set appropriate retry policies

3. **ROI Metrics**:
   - Review metrics weekly
   - Adjust baseline times based on actual measurements
   - Use metrics to prioritize automation

---

## Related Documentation

- [API Reference](../API-REFERENCE.md)
- [Developer Guide](../handbooks/03-DEVELOPER-GUIDE.md)
- [Epic 7 Stories](../../docs/stories/epic-7-batch-operations-webhooks.md)

---

**Last Updated:** 2025-12-06

