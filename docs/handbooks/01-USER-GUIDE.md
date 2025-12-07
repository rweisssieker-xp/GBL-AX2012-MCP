---
title: GBL-AX2012-MCP User Guide
description: Complete guide for end users to interact with AX 2012 via MCP
author: Paige (Technical Writer)
date: 2025-12-06
version: 1.4.0
---

# GBL-AX2012-MCP User Guide

## Overview

GBL-AX2012-MCP enables you to interact with Microsoft Dynamics AX 2012 R3 using natural language through AI assistants like Claude. Instead of navigating complex AX menus, you can simply ask for what you need.

**What you can do:**

- Create and manage sales orders
- Check inventory and pricing
- Process shipments and invoices
- Handle payments and settlements
- Query customer information and aging reports

---

## Getting Started

### For Claude Desktop Users

1. Ensure your IT administrator has configured the MCP server
2. Open Claude Desktop
3. The AX 2012 tools appear automatically in your tool list
4. Start with a simple query like: *"Check the health status of AX"*

### For Chat/Conversational Users

Simply type your request in natural language:

```
"Show me customer CUST-001"
"Create a sales order for Müller GmbH, 50 units of Widget Pro"
"What's the inventory status for item ITEM-100?"
```

---

## Common Tasks

### Looking Up a Customer

**What to say:**
> "Get customer details for account CUST-001"

**Or search by name:**
> "Find customers with name containing 'Müller'"

**What you get:**

| Field | Example |
|-------|---------|
| Account | CUST-001 |
| Name | Müller GmbH |
| Credit Limit | €100,000 |
| Balance | €25,000 |
| Status | Active |

---

### Checking Inventory

**What to say:**
> "Check inventory for item ITEM-100"

**Or specify a warehouse:**
> "Check inventory for ITEM-100 in warehouse WH-MAIN"

**What you get:**

| Field | Value |
|-------|-------|
| Available | 150 units |
| Reserved | 30 units |
| On Order | 200 units |

---

### Getting a Price Quote

**What to say:**
> "Simulate price for customer CUST-001, item ITEM-100, quantity 50"

**What you get:**

| Field | Value |
|-------|-------|
| Base Price | €25.00 |
| Customer Discount | 10% |
| Final Unit Price | €22.50 |
| Line Amount | €1,125.00 |

---

### Creating a Sales Order

**What to say:**
> "Create sales order for customer CUST-001 with 50 units of ITEM-100"

**What you get:**

| Field | Value |
|-------|-------|
| Sales ID | SO-2024-1234 |
| Status | Open |
| Total | €1,125.00 |

**Important:** Orders over €50,000 require approval. You'll receive an approval ID to track.

---

### Checking Order Status

**What to say:**
> "Show me sales order SO-2024-1234"

**Or list all orders for a customer:**
> "List all open orders for customer CUST-001"

---

### Reserving Inventory

**What to say:**
> "Reserve inventory for sales order SO-2024-1234, line 1"

**What you get:**

| Field | Value |
|-------|-------|
| Reserved Quantity | 50 |
| Warehouse | WH-MAIN |
| Status | Reserved |

---

### Posting a Shipment

**What to say:**
> "Post shipment for sales order SO-2024-1234"

**What you get:**

| Field | Value |
|-------|-------|
| Packing Slip | PS-2024-5678 |
| Ship Date | 2024-12-06 |
| Status | Shipped |

---

### Creating an Invoice

**What to say:**
> "Create invoice for sales order SO-2024-1234"

**What you get:**

| Field | Value |
|-------|-------|
| Invoice ID | INV-2024-9012 |
| Amount | €1,125.00 |
| Due Date | 2025-01-05 |

---

### Checking Customer Aging

**What to say:**
> "Show aging report for customer CUST-001"

**What you get:**

| Bucket | Amount |
|--------|--------|
| Current | €500.00 |
| 1-30 Days | €200.00 |
| 31-60 Days | €0.00 |
| 61-90 Days | €0.00 |
| 90+ Days | €0.00 |
| **Total Open** | **€700.00** |

---

### Posting a Payment

**What to say:**
> "Post payment of €1,125.00 for customer CUST-001, reference BANK-2024-001"

**What you get:**

| Field | Value |
|-------|-------|
| Payment ID | PAY-2024-3456 |
| Amount | €1,125.00 |
| Status | Posted |

---

### Settling an Invoice

**What to say:**
> "Settle invoice INV-2024-9012 with payment PAY-2024-3456"

**What you get:**

| Field | Value |
|-------|-------|
| Settlement ID | SET-2024-7890 |
| Amount Settled | €1,125.00 |
| Remaining | €0.00 |

---

### Closing a Sales Order

**What to say:**
> "Close sales order SO-2024-1234"

**Requirements:**
- All lines must be fully shipped
- All invoices must be settled

---

## Advanced Features

### Adding Lines to Existing Orders

**What to say:**
> "Add 25 units of ITEM-200 to sales order SO-2024-1234"

### Updating Delivery Dates

**What to say:**
> "Change delivery date for SO-2024-1234 to 2024-12-15"

### Checking Availability Forecast

**What to say:**
> "When will item ITEM-100 be available for 100 units?"

### Credit Check

**What to say:**
> "Check credit status for customer CUST-001 for a €75,000 order"

---

## Approval Workflow

### When Approval is Required

- Orders exceeding €50,000
- Credit limit overrides
- Special pricing requests

### Requesting Approval

**What to say:**
> "Request approval for sales order SO-2024-1234"

**What you get:**

| Field | Value |
|-------|-------|
| Approval ID | APR-2024-001 |
| Status | Pending |
| Approver | Finance Team |

### Checking Approval Status

**What to say:**
> "Check approval status for APR-2024-001"

---

## Tips for Best Results

### Be Specific

✅ **Good:** "Create sales order for CUST-001 with 50 units of ITEM-100"

❌ **Vague:** "Make an order"

### Use Identifiers

When you know the exact ID, use it:
- Customer: `CUST-001`
- Item: `ITEM-100`
- Sales Order: `SO-2024-1234`

### Confirm Before Writing

For important operations, the system will ask for confirmation:

> "I'm about to create a sales order for €15,000. Proceed? (yes/no)"

---

## Troubleshooting

### "Customer not found"

- Verify the customer account number
- Try searching by name instead

### "Insufficient inventory"

- Check current stock levels
- Consider partial fulfillment
- Check availability forecast

### "Credit limit exceeded"

- Contact Finance for approval
- Consider splitting the order
- Request credit limit increase

### "Order cannot be modified"

- Check order status (must be Open)
- Shipped orders cannot be changed

### "Approval required"

- High-value orders need Finance approval
- Use `ax_request_approval` to initiate
- Track with `ax_get_approval_status`

---

## Quick Reference Card

| Task | Command Example |
|------|-----------------|
| Health Check | "Check AX health status" |
| Get Customer | "Show customer CUST-001" |
| Search Customer | "Find customers named Müller" |
| Check Inventory | "Inventory for ITEM-100" |
| Get Price | "Price for CUST-001, ITEM-100, qty 50" |
| Create Order | "Create order for CUST-001 with ITEM-100 x 50" |
| Get Order | "Show order SO-2024-1234" |
| Reserve Stock | "Reserve line 1 of SO-2024-1234" |
| Ship Order | "Post shipment for SO-2024-1234" |
| Create Invoice | "Invoice order SO-2024-1234" |
| Customer Aging | "Aging for CUST-001" |
| Post Payment | "Post €1000 payment for CUST-001" |
| Settle Invoice | "Settle INV-001 with PAY-001" |
| Close Order | "Close order SO-2024-1234" |

---

## Getting Help

- **IT Support:** Contact your IT administrator for access issues
- **Business Questions:** Contact your supervisor for process questions
- **System Issues:** Report errors with the exact error message

---

*Document Version: 1.4.0 | Last Updated: 2025-12-06*
