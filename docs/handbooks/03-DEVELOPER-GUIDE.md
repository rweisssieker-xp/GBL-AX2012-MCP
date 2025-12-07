---
title: GBL-AX2012-MCP Developer Guide
description: Technical guide for developers extending and maintaining the MCP server
author: Paige (Technical Writer)
date: 2025-12-07
version: 1.5.0
---

# GBL-AX2012-MCP Developer Guide

## Overview

This guide covers the technical architecture, development setup, and extension patterns for the GBL-AX2012-MCP server.

---

## Architecture

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                          MCP Server                                   │
│  ┌─────────────────────────────────────────────────────────────────┐│
│  │                      Transport Layer                              ││
│  │  ┌──────────┐  ┌──────────────┐  ┌────────────────┐             ││
│  │  │  stdio   │  │ HTTP (8080)  │  │ Metrics (9090) │             ││
│  │  └──────────┘  └──────────────┘  └────────────────┘             ││
│  └─────────────────────────────────────────────────────────────────┘│
│  ┌─────────────────────────────────────────────────────────────────┐│
│  │                      Middleware Layer                             ││
│  │  ┌────────────┐  ┌───────────────┐  ┌──────────────────┐        ││
│  │  │Rate Limiter│  │Circuit Breaker│  │ Authentication   │        ││
│  │  └────────────┘  └───────────────┘  └──────────────────┘        ││
│  │  ┌────────────┐  ┌───────────────┐  ┌──────────────────┐        ││
│  │  │ Audit Log  │  │  Idempotency  │  │  Kill Switch     │        ││
│  │  └────────────┘  └───────────────┘  └──────────────────┘        ││
│  └─────────────────────────────────────────────────────────────────┘│
│  ┌─────────────────────────────────────────────────────────────────┐│
│  │                        Tool Layer                                 ││
│  │  26 Tools: Health, Customer, Order, Inventory, Price, etc.      ││
│  └─────────────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────────┘
                                  │
        ┌─────────────────────────┼─────────────────────────┐
        ▼                         ▼                         ▼
┌───────────────┐        ┌───────────────┐        ┌───────────────┐
│   AIF Client  │        │   WCF Client  │        │ BC.NET Client │
│   (Reads)     │        │   (Writes)    │        │   (Health)    │
└───────────────┘        └───────────────┘        └───────────────┘
        │                         │                         │
        └─────────────────────────┼─────────────────────────┘
                                  ▼
                        ┌───────────────────┐
                        │  AX 2012 R3 AOS   │
                        └───────────────────┘
```

### Project Structure

```
GBL.AX2012.MCP/
├── src/
│   ├── GBL.AX2012.MCP.Core/           # Shared interfaces, models, options
│   │   ├── Interfaces/                 # ITool, IAuditService, etc.
│   │   ├── Models/                     # Customer, SalesOrder, Item, etc.
│   │   ├── Options/                    # Configuration classes
│   │   └── Exceptions/                 # Custom exceptions
│   │
│   ├── GBL.AX2012.MCP.Server/         # Main server application
│   │   ├── Tools/                      # All 26 tool implementations
│   │   ├── Middleware/                 # Rate limiter, circuit breaker
│   │   ├── Security/                   # Authentication, authorization
│   │   ├── Transport/                  # HTTP transport
│   │   ├── Metrics/                    # Prometheus metrics
│   │   ├── Monitoring/                 # Health monitor service
│   │   ├── Notifications/              # Slack/Teams alerts
│   │   ├── Approval/                   # Approval workflow
│   │   └── Program.cs                  # Entry point, DI setup
│   │
│   ├── GBL.AX2012.MCP.AxConnector/    # AX integration clients
│   │   ├── Clients/                    # AifClient, WcfClient, BC
│   │   └── Interfaces/                 # Client interfaces
│   │
│   └── GBL.AX2012.MCP.Audit/          # Audit logging
│       ├── Services/                   # FileAuditService, EfCoreAudit
│       └── Data/                       # EF Core DbContext
│
├── tests/
│   ├── GBL.AX2012.MCP.Server.Tests/
│   ├── GBL.AX2012.MCP.AxConnector.Tests/
│   └── GBL.AX2012.MCP.Integration.Tests/
│
├── docs/
│   ├── handbooks/                      # User documentation
│   └── analysis/                       # Product brief, brainstorming
│
├── monitoring/
│   ├── prometheus.yml                  # Prometheus config
│   └── grafana/                        # Grafana dashboards
│
├── n8n-workflows/                      # n8n workflow templates
│
├── Dockerfile
├── docker-compose.yml
└── GBL.AX2012.MCP.sln
```

---

## Development Setup

### Prerequisites

- .NET 8.0 SDK
- Visual Studio 2022 or VS Code with C# extension
- Git
- (Optional) Docker Desktop
- (Optional) Access to AX 2012 R3 development environment

### Clone and Build

```bash
git clone <repository-url>
cd GBL-AX2012-MCP

# Restore dependencies
dotnet restore

# Build all projects
dotnet build

# Run tests
dotnet test
```

### Run Locally

```bash
# Run the MCP server
dotnet run --project src/GBL.AX2012.MCP.Server

# Or with specific configuration
dotnet run --project src/GBL.AX2012.MCP.Server -- --environment Development
```

### Configuration

Edit `src/GBL.AX2012.MCP.Server/appsettings.json`:

```json
{
  "McpServer": {
    "Transport": "stdio",
    "ServerName": "gbl-ax2012-mcp",
    "ServerVersion": "1.4.0"
  },
  "HttpTransport": {
    "Port": 8080,
    "Enabled": true,
    "AllowedOrigins": ["*"]
  },
  "AifClient": {
    "BaseUrl": "http://ax-aos:8101/DynamicsAx/Services",
    "Timeout": "00:00:30",
    "Company": "DAT"
  },
  "WcfClient": {
    "BaseUrl": "http://ax-aos:8101/DynamicsAx/Services/GblSalesOrderService",
    "Timeout": "00:00:30"
  },
  "RateLimiter": {
    "RequestsPerMinute": 100,
    "BurstSize": 20
  },
  "CircuitBreaker": {
    "FailureThreshold": 3,
    "OpenDuration": "00:01:00"
  },
  "HealthMonitor": {
    "CheckIntervalSeconds": 30,
    "ErrorRateThresholdPercent": 5,
    "Enabled": true
  }
}
```

---

## Creating a New Tool

### Step 1: Define Input/Output Models

```csharp
// In Tools/MyNewTool.cs

public class MyNewToolInput
{
    public string RequiredParam { get; set; } = "";
    public string? OptionalParam { get; set; }
    public decimal NumericParam { get; set; }
}

public class MyNewToolOutput
{
    public bool Success { get; set; }
    public string ResultId { get; set; } = "";
    public string Message { get; set; } = "";
}
```

### Step 2: Create Validator

```csharp
public class MyNewToolInputValidator : AbstractValidator<MyNewToolInput>
{
    public MyNewToolInputValidator()
    {
        RuleFor(x => x.RequiredParam)
            .NotEmpty()
            .WithMessage("RequiredParam is required");
            
        RuleFor(x => x.NumericParam)
            .GreaterThan(0)
            .WithMessage("NumericParam must be positive");
    }
}
```

### Step 3: Implement the Tool

```csharp
public class MyNewTool : ToolBase<MyNewToolInput, MyNewToolOutput>
{
    private readonly ILogger<MyNewTool> _logger;
    private readonly IAifClient _aifClient;
    private readonly MyNewToolInputValidator _validator;
    
    public MyNewTool(
        ILogger<MyNewTool> logger,
        IAifClient aifClient,
        MyNewToolInputValidator validator,
        IAuditService auditService)
        : base(auditService)
    {
        _logger = logger;
        _aifClient = aifClient;
        _validator = validator;
    }
    
    public override string Name => "ax_my_new_tool";
    public override string Description => "Description of what this tool does";
    
    public override ToolCategory Category => ToolCategory.Read; // or Write
    
    public override IEnumerable<string> RequiredRoles => new[] { "MCP_Read" };
    
    protected override async Task<MyNewToolOutput> ExecuteInternalAsync(
        MyNewToolInput input,
        ToolContext context,
        CancellationToken cancellationToken)
    {
        // Validate input
        var validation = await _validator.ValidateAsync(input, cancellationToken);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }
        
        _logger.LogInformation("Executing MyNewTool for {Param}", input.RequiredParam);
        
        // Call AX
        var result = await _aifClient.SomeMethodAsync(input.RequiredParam, cancellationToken);
        
        return new MyNewToolOutput
        {
            Success = true,
            ResultId = result.Id,
            Message = "Operation completed successfully"
        };
    }
}
```

### Step 4: Register in Program.cs

```csharp
// In Program.cs

// Register validator
builder.Services.AddSingleton<MyNewToolInputValidator>();

// Register tool
builder.Services.AddSingleton<ITool, MyNewTool>();
```

### Step 5: Add Tests

```csharp
// In tests/GBL.AX2012.MCP.Server.Tests/Tools/MyNewToolTests.cs

public class MyNewToolTests
{
    [Fact]
    public async Task ExecuteAsync_ValidInput_ReturnsSuccess()
    {
        // Arrange
        var mockAifClient = new Mock<IAifClient>();
        mockAifClient
            .Setup(x => x.SomeMethodAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SomeResult { Id = "TEST-001" });
            
        var tool = new MyNewTool(
            Mock.Of<ILogger<MyNewTool>>(),
            mockAifClient.Object,
            new MyNewToolInputValidator(),
            Mock.Of<IAuditService>());
            
        var input = new MyNewToolInput { RequiredParam = "test", NumericParam = 10 };
        var context = new ToolContext { User = "test-user", Company = "DAT" };
        
        // Act
        var result = await tool.ExecuteAsync(input, context, CancellationToken.None);
        
        // Assert
        Assert.True(result.Success);
        Assert.Equal("TEST-001", result.ResultId);
    }
    
    [Fact]
    public async Task ExecuteAsync_InvalidInput_ThrowsValidationException()
    {
        // Arrange
        var tool = new MyNewTool(/* ... */);
        var input = new MyNewToolInput { RequiredParam = "", NumericParam = -1 };
        
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => tool.ExecuteAsync(input, new ToolContext(), CancellationToken.None));
    }
}
```

---

## Core Interfaces

### ITool

```csharp
public interface ITool
{
    string Name { get; }
    string Description { get; }
    ToolCategory Category { get; }
    IEnumerable<string> RequiredRoles { get; }
    Type InputType { get; }
    Type OutputType { get; }
    
    Task<object> ExecuteAsync(
        object input, 
        ToolContext context, 
        CancellationToken cancellationToken);
}
```

### IAifClient

```csharp
public interface IAifClient
{
    Task<Customer?> GetCustomerAsync(string customerAccount, CancellationToken ct);
    Task<IEnumerable<Customer>> SearchCustomersAsync(string searchName, CancellationToken ct);
    Task<SalesOrder?> GetSalesOrderAsync(string salesId, CancellationToken ct);
    Task<IEnumerable<SalesOrder>> GetSalesOrdersByCustomerAsync(string customerAccount, CancellationToken ct);
    Task<InventoryOnHand?> GetInventoryAsync(string itemId, string? warehouseId, CancellationToken ct);
    Task<PriceResult?> SimulatePriceAsync(string customerAccount, string itemId, decimal qty, CancellationToken ct);
    Task<Item?> GetItemAsync(string itemId, CancellationToken ct);
}
```

### IWcfClient

```csharp
public interface IWcfClient
{
    Task<string> CreateSalesOrderAsync(CreateSalesOrderRequest request, CancellationToken ct);
    Task<bool> UpdateSalesOrderAsync(UpdateSalesOrderRequest request, CancellationToken ct);
    Task<int> AddSalesLineAsync(SalesLineCreateRequest request, CancellationToken ct);
}
```

### IAuditService

```csharp
public interface IAuditService
{
    Task LogAsync(AuditEntry entry, CancellationToken ct);
    Task<IEnumerable<AuditEntry>> QueryAsync(AuditQuery query, CancellationToken ct);
    Task<AuditStatistics> GetStatisticsAsync(DateTime from, DateTime to, CancellationToken ct);
}
```

---

## Middleware

### Rate Limiter

Limits requests per user per minute.

```csharp
public interface IRateLimiter
{
    Task<bool> TryAcquireAsync(string userId, CancellationToken ct);
    Task<RateLimitStatus> GetStatusAsync(string userId, CancellationToken ct);
}
```

**Configuration:**

```json
{
  "RateLimiter": {
    "RequestsPerMinute": 100,
    "BurstSize": 20
  }
}
```

### Circuit Breaker

Prevents cascading failures when AX is unavailable.

```csharp
public interface ICircuitBreaker
{
    CircuitState State { get; }
    Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken ct);
}

public enum CircuitState
{
    Closed,    // Normal operation
    Open,      // Failing, rejecting requests
    HalfOpen   // Testing if recovered
}
```

**Configuration:**

```json
{
  "CircuitBreaker": {
    "FailureThreshold": 3,
    "OpenDuration": "00:01:00"
  }
}
```

### Kill Switch

Emergency stop for all or specific tools.

```csharp
public interface IKillSwitchService
{
    bool IsActive { get; }
    string? Scope { get; }
    void Activate(string scope, string reason, string activatedBy);
    void Deactivate(string deactivatedBy);
    KillSwitchStatus GetStatus();
}
```

---

## Testing

### Unit Tests

```bash
# Run all unit tests
dotnet test tests/GBL.AX2012.MCP.Server.Tests

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Integration Tests

Integration tests use mock AX clients:

```csharp
public class MockAifClient : IAifClient
{
    private readonly Dictionary<string, Customer> _customers = new()
    {
        ["CUST-001"] = new Customer { CustomerAccount = "CUST-001", Name = "Test Customer" }
    };
    
    public Task<Customer?> GetCustomerAsync(string customerAccount, CancellationToken ct)
    {
        _customers.TryGetValue(customerAccount, out var customer);
        return Task.FromResult(customer);
    }
    // ... other methods
}
```

```bash
# Run integration tests
dotnet test tests/GBL.AX2012.MCP.Integration.Tests
```

### Test Fixture

```csharp
public class TestFixture : IDisposable
{
    public IServiceProvider Services { get; }
    
    public TestFixture()
    {
        var services = new ServiceCollection();
        
        // Register mocks
        services.AddSingleton<IAifClient, MockAifClient>();
        services.AddSingleton<IWcfClient, MockWcfClient>();
        services.AddSingleton<IBusinessConnector, MockBusinessConnector>();
        
        // Register real services
        services.AddSingleton<IRateLimiter, RateLimiter>();
        services.AddSingleton<ICircuitBreaker, CircuitBreaker>();
        
        // Register tools
        services.AddSingleton<ITool, GetCustomerTool>();
        // ... other tools
        
        Services = services.BuildServiceProvider();
    }
}
```

---

## Extending AX Connectors

### Adding a New AIF Method

1. Add method to `IAifClient`:

```csharp
public interface IAifClient
{
    // Existing methods...
    Task<NewEntity?> GetNewEntityAsync(string id, CancellationToken ct);
}
```

2. Implement in `AifClient`:

```csharp
public async Task<NewEntity?> GetNewEntityAsync(string id, CancellationToken ct)
{
    return await _circuitBreaker.ExecuteAsync(async () =>
    {
        var response = await _httpClient.GetAsync(
            $"{_options.BaseUrl}/NewEntityService/read?id={id}", ct);
            
        if (!response.IsSuccessStatusCode)
            return null;
            
        var content = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<NewEntity>(content);
    }, ct);
}
```

3. Add to mock client for testing:

```csharp
public class MockAifClient : IAifClient
{
    public Task<NewEntity?> GetNewEntityAsync(string id, CancellationToken ct)
    {
        return Task.FromResult<NewEntity?>(new NewEntity { Id = id });
    }
}
```

---

## Monitoring & Observability

### Prometheus Metrics

Available at `http://localhost:9090/metrics`:

| Metric | Type | Description |
|--------|------|-------------|
| `mcp_requests_total` | Counter | Total requests by tool |
| `mcp_request_duration_seconds` | Histogram | Request latency |
| `mcp_errors_total` | Counter | Errors by type |
| `mcp_circuit_breaker_state` | Gauge | Circuit breaker state |
| `mcp_rate_limit_rejections` | Counter | Rate limit rejections |
| `mcp_aos_connectivity` | Gauge | AOS connection status |

### Health Checks

```http
GET /health
```

```json
{
  "status": "healthy",
  "timestamp": "2024-12-06T10:30:00Z",
  "components": {
    "aif": "connected",
    "wcf": "connected",
    "businessConnector": "connected"
  },
  "metrics": {
    "uptime": "2d 5h 30m",
    "requestsToday": 1250,
    "errorRate": "1.2%"
  }
}
```

### Logging

Logs are written to `logs/mcp-{date}.log` using Serilog:

```
2024-12-06 10:30:00.123 +01:00 [INF] Executing ax_get_customer for CUST-001 {"User": "DOMAIN\\user"}
2024-12-06 10:30:00.245 +01:00 [INF] Customer found: Müller GmbH {"DurationMs": 122}
```

---

## Deployment

### Docker

```bash
# Build image
docker build -t gbl-ax2012-mcp:1.4.0 .

# Run container
docker run -d \
  -p 8080:8080 \
  -p 9090:9090 \
  -e AifClient__BaseUrl=http://ax-aos:8101/DynamicsAx/Services \
  gbl-ax2012-mcp:1.4.0
```

### Docker Compose

```bash
# Start all services (MCP, Prometheus, Grafana)
docker-compose up -d

# View logs
docker-compose logs -f mcp-server

# Stop
docker-compose down
```

### Windows Service

```bash
# Publish
dotnet publish -c Release -o ./publish

# Install as service (requires Administrator)
sc create GBL-AX2012-MCP binPath="C:\path\to\publish\GBL.AX2012.MCP.Server.exe"
sc start GBL-AX2012-MCP
```

---

## Troubleshooting

### Common Issues

**Build fails with missing references:**
```bash
dotnet restore --force
dotnet build --no-incremental
```

**Tests fail with timeout:**
- Check if AX mock clients are properly configured
- Increase test timeout in test settings

**Circuit breaker stays open:**
- Check AX connectivity
- Review logs for root cause
- Manually reset via kill switch

### Debug Mode

```bash
# Run with debug logging
dotnet run --project src/GBL.AX2012.MCP.Server -- --environment Development
```

Set in `appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "GBL.AX2012.MCP": "Trace"
    }
  }
}
```

---

## Contributing

### Code Style

- Follow C# coding conventions
- Use meaningful names
- Add XML documentation for public APIs
- Keep methods focused and small

### Pull Request Process

1. Create feature branch from `main`
2. Implement changes with tests
3. Ensure all tests pass
4. Update documentation if needed
5. Submit PR with clear description

### Commit Messages

```
feat: Add ax_new_tool for feature X
fix: Resolve null reference in GetCustomerTool
docs: Update API reference for v1.4.0
test: Add integration tests for approval workflow
```

---

*Document Version: 1.4.0 | Last Updated: 2025-12-06*
