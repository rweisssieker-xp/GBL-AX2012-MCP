# GBL-AX2012-MCP

**Model Context Protocol (MCP) Server for Microsoft Dynamics AX 2012 R3**

A production-ready MCP server that enables AI assistants to interact with AX 2012 R3 for **complete Order-to-Cash (O2C) automation**.

## Features

### Tools - Full O2C Coverage

#### Phase 1: Order Capture
| Tool | Description | Role |
|------|-------------|------|
| `ax_health_check` | Check server and AX connectivity | Read |
| `ax_get_customer` | Get customer by account or search by name | Read |
| `ax_get_salesorder` | Get sales order by ID or list by customer | Read |
| `ax_check_inventory` | Check item availability | Read |
| `ax_simulate_price` | Simulate pricing without creating order | Read |
| `ax_create_salesorder` | Create new sales order | Write |

#### Phase 2: Fulfillment
| Tool | Description | Role |
|------|-------------|------|
| `ax_reserve_salesline` | Reserve inventory for order line | Write |
| `ax_post_shipment` | Post shipment/packing slip | Write |

#### Phase 3: Invoice & Dunning
| Tool | Description | Role |
|------|-------------|------|
| `ax_create_invoice` | Create and post invoice | Write |
| `ax_get_customer_aging` | Get AR aging and open invoices | Read |

#### Phase 4: Payment & Close
| Tool | Description | Role |
|------|-------------|------|
| `ax_post_payment` | Post customer payment | Write |
| `ax_settle_invoice` | Settle invoice against payment | Write |
| `ax_close_salesorder` | Close completed order | Write |

#### Approval Workflow
| Tool | Description | Role |
|------|-------------|------|
| `ax_request_approval` | Request approval for high-value operations | Write |
| `ax_get_approval_status` | Check approval status | Read |

#### Batch Operations & Webhooks (NEW)
| Tool | Description | Role |
|------|-------------|------|
| `ax_batch_operations` | Execute multiple operations in a single call | Write |
| `ax_subscribe_webhook` | Subscribe to MCP events via webhooks | Admin |
| `ax_list_webhooks` | List all webhook subscriptions | Admin |
| `ax_unsubscribe_webhook` | Unsubscribe from a webhook | Admin |
| `ax_get_roi_metrics` | Get ROI metrics for MCP operations | Admin |
| `ax_bulk_import` | Import data from CSV/JSON | Write |

#### Self-Healing (NEW)
| Tool | Description | Role |
|------|-------------|------|
| `ax_get_self_healing_status` | Get status of self-healing components | Admin |

### Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        MCP Server (.NET 8)                      │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────┐  │
│  │ Rate Limiter│  │Circuit Break│  │ Windows Authentication  │  │
│  └─────────────┘  └─────────────┘  └─────────────────────────┘  │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │                         Tools                                ││
│  │  HealthCheck │ GetCustomer │ GetSalesOrder │ CheckInventory ││
│  │  SimulatePrice │ CreateSalesOrder │ BatchOps │ Webhooks     ││
│  └─────────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────┘
                              │
        ┌─────────────────────┼─────────────────────┐
        ▼                     ▼                     ▼
┌───────────────┐    ┌───────────────┐    ┌──────────────────────┐
│ AIF Client    │    │ WCF Client    │    │ BC.Wrapper Service  │
│ HTTP/NetTcp   │    │ (Writes)      │    │ (.NET Framework)    │
│ (Reads)       │    │               │    │ → BC.NET → AX       │
└───────────────┘    └───────────────┘    └──────────────────────┘
        │                     │                     │
        └─────────────────────┼─────────────────────┘
                              ▼
                    ┌───────────────────┐
                    │  AX 2012 R3 AOS   │
                    └───────────────────┘
```

**Features:**
- ✅ **Automatic Fallback:** HTTP → NetTcp for AIF
- ✅ **BC.Wrapper:** .NET Framework bridge for BC.NET
- ✅ **Configuration Validation:** Startup checks
- ✅ **Self-Healing:** Automatic recovery

### Multi-Channel Transport

| Transport | Port | Description |
|-----------|------|-------------|
| **stdio** | - | Standard MCP protocol for Claude Desktop |
| **HTTP** | 8080 | REST API for n8n, webhooks, custom integrations |
| **Metrics** | 9090 | Prometheus metrics endpoint |

## Quick Start

### Prerequisites

- .NET 8.0 SDK
- Microsoft Dynamics AX 2012 R3 CU13
- Windows Authentication configured

### Build

```bash
dotnet restore
dotnet build
```

### Run

```bash
dotnet run --project src/GBL.AX2012.MCP.Server
```

### Test

```bash
dotnet test
```

### HTTP API Usage

```bash
# List tools
curl http://localhost:8080/tools

# Call a tool
curl -X POST http://localhost:8080/tools/call \
  -H "Content-Type: application/json" \
  -d '{"tool": "ax_get_customer", "arguments": {"customerAccount": "CUST-001"}}'

# Health check
curl http://localhost:8080/health

# Prometheus metrics
curl http://localhost:9090/metrics
```

## Configuration

Edit `appsettings.json`:

```json
{
  "McpServer": {
    "ServerName": "gbl-ax2012-mcp",
    "ServerVersion": "1.6.0"
  },
  "AifClient": {
    "BaseUrl": "http://ax-aos:8101/DynamicsAx/Services",
    "Company": "DAT",
    "UseNetTcp": false,
    "NetTcpPort": 8201,
    "FallbackStrategy": "auto"
  },
  "BusinessConnector": {
    "ObjectServer": "ax-aos:2712",
    "Company": "DAT",
    "UseWrapper": true,
    "WrapperUrl": "http://localhost:8090"
  },
  "RateLimiter": {
    "RequestsPerMinute": 100
  },
  "CircuitBreaker": {
    "FailureThreshold": 3,
    "OpenDuration": "00:01:00"
  },
  "ConnectionStrings": {
    "AuditDb": "Server=localhost;Database=MCP_Audit;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

### New Features

**NetTcp Support:**
- Automatic fallback from HTTP to NetTcp
- Configurable via `FallbackStrategy`: "auto", "http", or "nettcp"
- See `docs/AIF-NETTCP-SETUP.md`

**BC.Wrapper:**
- .NET Framework service for Business Connector .NET
- Required for .NET 8 compatibility
- See `docs/BC-WRAPPER-SETUP.md`

**Configuration Validation:**
- Automatic validation on startup
- Checks database, AX connections, URLs
- Application won't start if validation fails

## Claude Desktop Integration

Add to `claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "ax2012": {
      "command": "dotnet",
      "args": ["run", "--project", "C:/path/to/GBL.AX2012.MCP.Server"]
    }
  }
}
```

## Security

- **Authentication**: Windows Authentication (NTLM/Kerberos)
- **Authorization**: Role-based (MCP_Read, MCP_Write, MCP_Admin)
- **Rate Limiting**: 100 requests/minute per user
- **Circuit Breaker**: Opens after 3 failures, 60s recovery
- **Audit**: All operations logged

## Project Structure

```
GBL.AX2012.MCP/
├── src/
│   ├── GBL.AX2012.MCP.Core/          # Interfaces, Models, Options
│   ├── GBL.AX2012.MCP.Server/        # MCP Server, Tools, Middleware
│   ├── GBL.AX2012.MCP.AxConnector/   # AIF, WCF, BC.NET clients
│   └── GBL.AX2012.MCP.Audit/         # Audit services
├── tests/
│   ├── GBL.AX2012.MCP.Server.Tests/
│   └── GBL.AX2012.MCP.AxConnector.Tests/
└── docs/                              # Documentation
```

## License

Proprietary - GBL Internal Use Only

## Author

Reinerw - 2025
