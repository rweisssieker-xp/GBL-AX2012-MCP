# GBL-AX2012-MCP

**Model Context Protocol (MCP) Server for Microsoft Dynamics AX 2012 R3**

A production-ready MCP server that enables AI assistants to interact with AX 2012 R3 for Order-to-Cash (O2C) automation.

## Features

### Tools

| Tool | Description | Role |
|------|-------------|------|
| `ax_health_check` | Check server and AX connectivity | Read |
| `ax_get_customer` | Get customer by account or search by name | Read |
| `ax_get_salesorder` | Get sales order by ID or list by customer | Read |
| `ax_check_inventory` | Check item availability | Read |
| `ax_simulate_price` | Simulate pricing without creating order | Read |
| `ax_create_salesorder` | Create new sales order | Write |

### Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        MCP Server                                │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────┐  │
│  │ Rate Limiter│  │Circuit Break│  │ Windows Authentication  │  │
│  └─────────────┘  └─────────────┘  └─────────────────────────┘  │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │                         Tools                                ││
│  │  HealthCheck │ GetCustomer │ GetSalesOrder │ CheckInventory ││
│  │  SimulatePrice │ CreateSalesOrder                           ││
│  └─────────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────┘
                              │
        ┌─────────────────────┼─────────────────────┐
        ▼                     ▼                     ▼
┌───────────────┐    ┌───────────────┐    ┌───────────────┐
│   AIF Client  │    │   WCF Client  │    │ BC.NET Client │
│   (Reads)     │    │   (Writes)    │    │   (Health)    │
└───────────────┘    └───────────────┘    └───────────────┘
        │                     │                     │
        └─────────────────────┼─────────────────────┘
                              ▼
                    ┌───────────────────┐
                    │  AX 2012 R3 AOS   │
                    └───────────────────┘
```

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

## Configuration

Edit `appsettings.json`:

```json
{
  "McpServer": {
    "ServerName": "gbl-ax2012-mcp",
    "ServerVersion": "1.0.0"
  },
  "AifClient": {
    "BaseUrl": "http://ax-aos:8101/DynamicsAx/Services",
    "Company": "DAT"
  },
  "RateLimiter": {
    "RequestsPerMinute": 100
  },
  "CircuitBreaker": {
    "FailureThreshold": 3,
    "OpenDuration": "00:01:00"
  }
}
```

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
