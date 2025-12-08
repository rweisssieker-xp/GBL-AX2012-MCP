# Event Publishing Integration

**Date:** 2025-12-06  
**Status:** ✅ COMPLETE

---

## Overview

All write operations now publish events that can trigger webhooks and other integrations.

---

## Integrated Tools

### 1. Create Sales Order (`ax_create_salesorder`)

**Event:** `SalesOrderCreatedEvent`

**Published When:** Order successfully created

**Event Data:**
```json
{
  "sales_id": "SO-2025-001234",
  "customer_account": "CUST-001",
  "total_amount": 5000.00,
  "created_at": "2025-12-06T14:30:00Z",
  "user_id": "CORP\\jsmith"
}
```

---

### 2. Update Sales Order (`ax_update_salesorder`)

**Event:** `SalesOrderUpdatedEvent`

**Published When:** Order successfully updated (only if changes occurred)

**Event Data:**
```json
{
  "sales_id": "SO-2025-001234",
  "customer_account": "CUST-001",
  "updated_at": "2025-12-06T14:35:00Z",
  "user_id": "CORP\\jsmith"
}
```

---

### 3. Post Payment (`ax_post_payment`)

**Event:** `PaymentPostedEvent`

**Published When:** Payment successfully posted

**Event Data:**
```json
{
  "payment_id": "PAY-20251206143000-CUST-001",
  "customer_account": "CUST-001",
  "amount": 5000.00,
  "posted_at": "2025-12-06T14:30:00Z",
  "user_id": "CORP\\jsmith"
}
```

---

### 4. Create Invoice (`ax_create_invoice`)

**Event:** `InvoiceCreatedEvent`

**Published When:** Invoice successfully created

**Event Data:**
```json
{
  "invoice_id": "INV-SO-2025-001234-20251206143000",
  "customer_account": "CUST-001",
  "amount": 5950.00,
  "created_at": "2025-12-06T14:30:00Z",
  "user_id": "CORP\\jsmith"
}
```

---

## Webhook Integration

All events automatically trigger webhooks if subscriptions exist:

1. **Subscribe to events:**
```json
{
  "tool": "ax_subscribe_webhook",
  "arguments": {
    "event_type": "salesorder.created",
    "webhook_url": "https://n8n.example.com/webhook/order"
  }
}
```

2. **Events are automatically delivered** when published

3. **Webhook payload includes full event data**

---

## Event Flow

```
Tool Execution
    │
    ├─▶ Success
    │       │
    │       ├─▶ Publish Event (EventBus)
    │       │       │
    │       │       └─▶ Webhook Service
    │       │               │
    │       │               └─▶ Deliver to Subscribers
    │       │
    │       └─▶ Return Response
    │
    └─▶ Failure
            │
            └─▶ No Event Published
```

---

## Implementation Details

### Event Bus

- **Type:** In-memory event bus
- **Pattern:** Publisher-Subscriber
- **Threading:** Async/await
- **Error Handling:** Fire-and-forget (errors logged, don't affect operation)

### Event Publishing

- **Non-blocking:** Events published asynchronously
- **Optional:** Tools work without EventBus (graceful degradation)
- **Idempotent:** Events can be published multiple times safely

### Webhook Delivery

- **Automatic:** Webhooks triggered automatically on event
- **Filtering:** Webhooks can filter by event data
- **Retry:** Automatic retry with exponential backoff
- **Signature:** HMAC-SHA256 for security

---

## Testing

### Manual Test

1. **Subscribe to event:**
```bash
curl -X POST http://localhost:8080/tools/call \
  -H "Content-Type: application/json" \
  -d '{
    "tool": "ax_subscribe_webhook",
    "arguments": {
      "event_type": "salesorder.created",
      "webhook_url": "https://webhook.site/unique-id"
    }
  }'
```

2. **Trigger event** by creating an order:
```bash
curl -X POST http://localhost:8080/tools/call \
  -H "Content-Type: application/json" \
  -d '{
    "tool": "ax_create_salesorder",
    "arguments": {
      "customer_account": "CUST-001",
      "lines": [...],
      "idempotency_key": "test-uuid"
    }
  }'
```

3. **Check webhook delivery** at webhook.site

---

## Future Enhancements

1. **Event Store:** Persist events for replay
2. **Event Filtering:** Advanced filter expressions
3. **Event Batching:** Batch multiple events
4. **Event Replay:** Replay events from history
5. **Event Versioning:** Support event schema evolution

---

**Last Updated:** 2025-12-06

