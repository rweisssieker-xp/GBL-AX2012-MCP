---
stepsCompleted: [1, 2, 3, 4, 5]
inputDocuments:
  - "docs/requirements/prd-GBL-AX2012-MCP-2025-12-06.md"
workflowType: "technical-spec"
status: "COMPLETED"
project_name: "GBL-AX2012-MCP"
user_name: "Reinerw"
date: "2025-12-06"
---

# Technical Specification: GBL-AX2012-MCP

**Version:** 1.0  
**Date:** 2025-12-06  
**Author:** Reinerw  
**Status:** Draft for Review

---

## 1. System Overview

### 1.1 Purpose

Der GBL-AX2012-MCP Server ist eine Middleware-Komponente, die Microsoft Dynamics AX 2012 R3 CU13 über das Model Context Protocol (MCP) für AI-Agenten und Workflow-Orchestrierung zugänglich macht.

### 1.2 Technology Stack

| Layer | Technology | Version |
|-------|------------|---------|
| Runtime | .NET | 8.0 LTS |
| MCP Protocol | MCP SDK | 1.x |
| AX Integration | Business Connector .NET | AX 2012 R3 |
| AX Integration | AIF Services | AX 2012 R3 |
| AX Integration | Custom WCF Services | .NET 4.8 |
| Logging | Serilog | 3.x |
| Configuration | Microsoft.Extensions.Configuration | 8.x |
| DI Container | Microsoft.Extensions.DependencyInjection | 8.x |

### 1.3 Deployment Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                      PRODUCTION ENVIRONMENT                          │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │                    APPLICATION SERVER                        │   │
│  │                                                              │   │
│  │  ┌─────────────────────────────────────────────────────┐    │   │
│  │  │           GBL-AX2012-MCP Server                      │    │   │
│  │  │           (Windows Service)                          │    │   │
│  │  │                                                      │    │   │
│  │  │  Port: 5000 (HTTP) / 5001 (HTTPS)                   │    │   │
│  │  │  Protocol: MCP over stdio/SSE                        │    │   │
│  │  └─────────────────────────────────────────────────────┘    │   │
│  │                         │                                    │   │
│  │                         │ Windows Auth                       │   │
│  │                         ▼                                    │   │
│  │  ┌─────────────────────────────────────────────────────┐    │   │
│  │  │           AX 2012 R3 AOS                             │    │   │
│  │  │           (Same Server or Network)                   │    │   │
│  │  │                                                      │    │   │
│  │  │  AIF Services: http://aos:8101/DynamicsAx/Services  │    │   │
│  │  │  Custom WCF:   http://aos:8102/GBL/Services         │    │   │
│  │  └─────────────────────────────────────────────────────┘    │   │
│  │                         │                                    │   │
│  │                         ▼                                    │   │
│  │  ┌─────────────────────────────────────────────────────┐    │   │
│  │  │           SQL Server                                 │    │   │
│  │  │           (AX Database + Audit Database)             │    │   │
│  │  └─────────────────────────────────────────────────────┘    │   │
│  │                                                              │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 2. Component Design

### 2.1 Project Structure

```
GBL.AX2012.MCP/
├── src/
│   ├── GBL.AX2012.MCP.Server/           # Main MCP Server
│   │   ├── Program.cs                    # Entry point
│   │   ├── McpServer.cs                  # MCP protocol handler
│   │   ├── Tools/                        # Tool implementations
│   │   │   ├── HealthCheckTool.cs
│   │   │   ├── GetCustomerTool.cs
│   │   │   ├── GetSalesOrderTool.cs
│   │   │   ├── CheckInventoryTool.cs
│   │   │   ├── SimulatePriceTool.cs
│   │   │   └── CreateSalesOrderTool.cs
│   │   ├── Middleware/                   # Cross-cutting concerns
│   │   │   ├── RateLimiter.cs
│   │   │   ├── CircuitBreaker.cs
│   │   │   ├── ValidationMiddleware.cs
│   │   │   └── AuditMiddleware.cs
│   │   └── appsettings.json
│   │
│   ├── GBL.AX2012.MCP.AxConnector/      # AX Integration Layer
│   │   ├── Interfaces/
│   │   │   ├── IAifClient.cs
│   │   │   ├── IWcfClient.cs
│   │   │   └── IBusinessConnector.cs
│   │   ├── Clients/
│   │   │   ├── AifClient.cs              # AIF Service calls
│   │   │   ├── WcfClient.cs              # Custom WCF calls
│   │   │   └── BusinessConnectorClient.cs
│   │   ├── Models/
│   │   │   ├── Customer.cs
│   │   │   ├── SalesOrder.cs
│   │   │   ├── SalesLine.cs
│   │   │   ├── InventoryOnHand.cs
│   │   │   └── PriceResult.cs
│   │   └── Mappers/
│   │       └── AxDataMapper.cs
│   │
│   ├── GBL.AX2012.MCP.Core/             # Shared Core
│   │   ├── Exceptions/
│   │   │   ├── AxException.cs
│   │   │   ├── ValidationException.cs
│   │   │   └── RateLimitException.cs
│   │   ├── Models/
│   │   │   ├── ToolRequest.cs
│   │   │   ├── ToolResponse.cs
│   │   │   └── AuditEntry.cs
│   │   └── Extensions/
│   │       └── ServiceCollectionExtensions.cs
│   │
│   └── GBL.AX2012.MCP.Audit/            # Audit & Logging
│       ├── IAuditService.cs
│       ├── DatabaseAuditService.cs
│       ├── FileAuditService.cs
│       └── AuditDbContext.cs
│
├── tests/
│   ├── GBL.AX2012.MCP.Server.Tests/
│   ├── GBL.AX2012.MCP.AxConnector.Tests/
│   └── GBL.AX2012.MCP.Integration.Tests/
│
├── docs/
│   └── api/
│
└── deploy/
    ├── Dockerfile
    ├── docker-compose.yml
    └── install-service.ps1
```

### 2.2 Core Classes

#### 2.2.1 McpServer

```csharp
namespace GBL.AX2012.MCP.Server;

public class McpServer : IHostedService
{
    private readonly ILogger<McpServer> _logger;
    private readonly IServiceProvider _services;
    private readonly McpServerOptions _options;
    
    public McpServer(
        ILogger<McpServer> logger,
        IServiceProvider services,
        IOptions<McpServerOptions> options)
    {
        _logger = logger;
        _services = services;
        _options = options.Value;
    }
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting MCP Server on {Transport}", _options.Transport);
        
        // Initialize MCP protocol handler
        // Register tools
        // Start listening
    }
    
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping MCP Server");
    }
}
```

#### 2.2.2 Tool Base Class

```csharp
namespace GBL.AX2012.MCP.Server.Tools;

public abstract class ToolBase<TInput, TOutput> : ITool
    where TInput : class
    where TOutput : class
{
    protected readonly ILogger _logger;
    protected readonly IAuditService _audit;
    protected readonly IValidator<TInput> _validator;
    
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract JsonSchema InputSchema { get; }
    
    protected ToolBase(
        ILogger logger,
        IAuditService audit,
        IValidator<TInput> validator)
    {
        _logger = logger;
        _audit = audit;
        _validator = validator;
    }
    
    public async Task<ToolResponse> ExecuteAsync(
        JsonElement input, 
        ToolContext context,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var auditEntry = new AuditEntry
        {
            ToolName = Name,
            UserId = context.UserId,
            Timestamp = DateTime.UtcNow,
            Input = input.ToString()
        };
        
        try
        {
            // Deserialize and validate input
            var typedInput = JsonSerializer.Deserialize<TInput>(input);
            var validationResult = await _validator.ValidateAsync(typedInput, cancellationToken);
            
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }
            
            // Execute tool-specific logic
            var output = await ExecuteCoreAsync(typedInput, context, cancellationToken);
            
            auditEntry.Success = true;
            auditEntry.Output = JsonSerializer.Serialize(output);
            auditEntry.DurationMs = stopwatch.ElapsedMilliseconds;
            
            return ToolResponse.Success(output);
        }
        catch (Exception ex)
        {
            auditEntry.Success = false;
            auditEntry.Error = ex.Message;
            auditEntry.DurationMs = stopwatch.ElapsedMilliseconds;
            
            _logger.LogError(ex, "Tool {ToolName} failed", Name);
            throw;
        }
        finally
        {
            await _audit.LogAsync(auditEntry, cancellationToken);
        }
    }
    
    protected abstract Task<TOutput> ExecuteCoreAsync(
        TInput input, 
        ToolContext context,
        CancellationToken cancellationToken);
}
```

#### 2.2.3 Create Sales Order Tool

```csharp
namespace GBL.AX2012.MCP.Server.Tools;

public class CreateSalesOrderTool : ToolBase<CreateSalesOrderInput, CreateSalesOrderOutput>
{
    private readonly IWcfClient _wcfClient;
    private readonly IAifClient _aifClient;
    private readonly IIdempotencyStore _idempotencyStore;
    
    public override string Name => "ax_create_salesorder";
    public override string Description => "Creates a new sales order in AX 2012";
    
    public override JsonSchema InputSchema => new JsonSchema
    {
        Type = "object",
        Properties = new Dictionary<string, JsonSchema>
        {
            ["customer_account"] = new() { Type = "string", Description = "Customer account ID" },
            ["requested_delivery"] = new() { Type = "string", Format = "date" },
            ["customer_ref"] = new() { Type = "string" },
            ["lines"] = new() 
            { 
                Type = "array",
                Items = new JsonSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, JsonSchema>
                    {
                        ["item_id"] = new() { Type = "string" },
                        ["quantity"] = new() { Type = "number" },
                        ["unit_price"] = new() { Type = "number" },
                        ["warehouse"] = new() { Type = "string" }
                    },
                    Required = ["item_id", "quantity"]
                }
            },
            ["idempotency_key"] = new() { Type = "string", Description = "Unique key for retry safety" }
        },
        Required = ["customer_account", "lines", "idempotency_key"]
    };
    
    public CreateSalesOrderTool(
        ILogger<CreateSalesOrderTool> logger,
        IAuditService audit,
        IValidator<CreateSalesOrderInput> validator,
        IWcfClient wcfClient,
        IAifClient aifClient,
        IIdempotencyStore idempotencyStore)
        : base(logger, audit, validator)
    {
        _wcfClient = wcfClient;
        _aifClient = aifClient;
        _idempotencyStore = idempotencyStore;
    }
    
    protected override async Task<CreateSalesOrderOutput> ExecuteCoreAsync(
        CreateSalesOrderInput input,
        ToolContext context,
        CancellationToken cancellationToken)
    {
        // 1. Check idempotency
        var existing = await _idempotencyStore.GetAsync(input.IdempotencyKey, cancellationToken);
        if (existing != null)
        {
            _logger.LogInformation("Returning cached result for idempotency key {Key}", input.IdempotencyKey);
            return existing;
        }
        
        // 2. Validate customer exists
        var customer = await _aifClient.GetCustomerAsync(input.CustomerAccount, cancellationToken);
        if (customer == null)
        {
            throw new AxException("CUST_NOT_FOUND", $"Customer {input.CustomerAccount} not found");
        }
        
        if (customer.Blocked)
        {
            throw new AxException("CUST_BLOCKED", $"Customer {input.CustomerAccount} is blocked");
        }
        
        // 3. Validate items exist
        foreach (var line in input.Lines)
        {
            var item = await _aifClient.GetItemAsync(line.ItemId, cancellationToken);
            if (item == null)
            {
                throw new AxException("ITEM_NOT_FOUND", $"Item {line.ItemId} not found");
            }
            
            if (item.BlockedForSales)
            {
                throw new AxException("ITEM_BLOCKED", $"Item {line.ItemId} is blocked for sales");
            }
        }
        
        // 4. Check credit limit
        var orderTotal = await CalculateOrderTotalAsync(input, cancellationToken);
        var creditAvailable = customer.CreditLimit - customer.CreditUsed;
        
        if (orderTotal > creditAvailable)
        {
            throw new AxException("CREDIT_EXCEEDED", 
                $"Order total {orderTotal:C} exceeds available credit {creditAvailable:C}");
        }
        
        // 5. Create order via WCF
        var salesId = await _wcfClient.CreateSalesOrderAsync(new AxSalesOrderRequest
        {
            CustomerAccount = input.CustomerAccount,
            RequestedDeliveryDate = input.RequestedDelivery ?? DateTime.Today.AddDays(7),
            CustomerRef = input.CustomerRef,
            Lines = input.Lines.Select(l => new AxSalesLineRequest
            {
                ItemId = l.ItemId,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                WarehouseId = l.Warehouse
            }).ToList()
        }, cancellationToken);
        
        // 6. Build response
        var output = new CreateSalesOrderOutput
        {
            Success = true,
            SalesId = salesId,
            CustomerAccount = input.CustomerAccount,
            OrderDate = DateTime.Today,
            TotalAmount = orderTotal,
            Currency = customer.Currency,
            LinesCreated = input.Lines.Count,
            Warnings = new List<string>(),
            AuditId = Guid.NewGuid().ToString()
        };
        
        // 7. Store for idempotency
        await _idempotencyStore.SetAsync(input.IdempotencyKey, output, TimeSpan.FromDays(7), cancellationToken);
        
        return output;
    }
    
    private async Task<decimal> CalculateOrderTotalAsync(
        CreateSalesOrderInput input, 
        CancellationToken cancellationToken)
    {
        decimal total = 0;
        
        foreach (var line in input.Lines)
        {
            if (line.UnitPrice.HasValue)
            {
                total += line.UnitPrice.Value * line.Quantity;
            }
            else
            {
                var price = await _aifClient.SimulatePriceAsync(
                    input.CustomerAccount, 
                    line.ItemId, 
                    line.Quantity, 
                    cancellationToken);
                total += price.LineAmount;
            }
        }
        
        return total;
    }
}
```

---

## 3. AX Integration Layer

### 3.1 AIF Client (Read Operations)

```csharp
namespace GBL.AX2012.MCP.AxConnector.Clients;

public class AifClient : IAifClient
{
    private readonly HttpClient _httpClient;
    private readonly AifClientOptions _options;
    private readonly ILogger<AifClient> _logger;
    
    public AifClient(
        HttpClient httpClient,
        IOptions<AifClientOptions> options,
        ILogger<AifClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        
        // Configure Windows Authentication
        _httpClient.DefaultRequestHeaders.Add("SOAPAction", "");
    }
    
    public async Task<Customer?> GetCustomerAsync(
        string customerAccount, 
        CancellationToken cancellationToken)
    {
        var soapRequest = BuildSoapRequest("CustCustomerService", "find", new
        {
            _criteria = new
            {
                CustTable = new { AccountNum = customerAccount }
            }
        });
        
        var response = await _httpClient.PostAsync(
            $"{_options.BaseUrl}/CustCustomerService",
            new StringContent(soapRequest, Encoding.UTF8, "text/xml"),
            cancellationToken);
        
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        return ParseCustomerResponse(content);
    }
    
    public async Task<SalesOrder?> GetSalesOrderAsync(
        string salesId, 
        CancellationToken cancellationToken)
    {
        var soapRequest = BuildSoapRequest("SalesSalesOrderService", "read", new
        {
            _entityKeyList = new[]
            {
                new { KeyData = new[] { new { Field = "SalesId", Value = salesId } } }
            }
        });
        
        var response = await _httpClient.PostAsync(
            $"{_options.BaseUrl}/SalesSalesOrderService",
            new StringContent(soapRequest, Encoding.UTF8, "text/xml"),
            cancellationToken);
        
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        return ParseSalesOrderResponse(content);
    }
    
    public async Task<InventoryOnHand> GetInventoryOnHandAsync(
        string itemId,
        string? warehouseId,
        CancellationToken cancellationToken)
    {
        // Use InventSumService or custom query service
        var soapRequest = BuildSoapRequest("InventInventSumService", "find", new
        {
            _criteria = new
            {
                InventSum = new 
                { 
                    ItemId = itemId,
                    InventDimId = warehouseId != null ? new { InventLocationId = warehouseId } : null
                }
            }
        });
        
        var response = await _httpClient.PostAsync(
            $"{_options.BaseUrl}/InventInventSumService",
            new StringContent(soapRequest, Encoding.UTF8, "text/xml"),
            cancellationToken);
        
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        return ParseInventoryResponse(content);
    }
    
    private string BuildSoapRequest(string service, string operation, object parameters)
    {
        // Build SOAP envelope with AX-specific namespaces
        return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
    <soap:Body>
        <{operation} xmlns=""http://schemas.microsoft.com/dynamics/2008/01/services"">
            {SerializeParameters(parameters)}
        </{operation}>
    </soap:Body>
</soap:Envelope>";
    }
}
```

### 3.2 WCF Client (Write Operations)

```csharp
namespace GBL.AX2012.MCP.AxConnector.Clients;

public class WcfClient : IWcfClient
{
    private readonly ChannelFactory<IGblSalesOrderService> _channelFactory;
    private readonly ILogger<WcfClient> _logger;
    private readonly ICircuitBreaker _circuitBreaker;
    
    public WcfClient(
        IOptions<WcfClientOptions> options,
        ILogger<WcfClient> logger,
        ICircuitBreaker circuitBreaker)
    {
        _logger = logger;
        _circuitBreaker = circuitBreaker;
        
        var binding = new BasicHttpBinding
        {
            Security = new BasicHttpSecurity
            {
                Mode = BasicHttpSecurityMode.TransportCredentialOnly,
                Transport = new HttpTransportSecurity
                {
                    ClientCredentialType = HttpClientCredentialType.Windows
                }
            },
            MaxReceivedMessageSize = 10 * 1024 * 1024, // 10 MB
            SendTimeout = TimeSpan.FromSeconds(30),
            ReceiveTimeout = TimeSpan.FromSeconds(30)
        };
        
        var endpoint = new EndpointAddress(options.Value.BaseUrl);
        _channelFactory = new ChannelFactory<IGblSalesOrderService>(binding, endpoint);
        
        // Use service account credentials
        _channelFactory.Credentials.Windows.ClientCredential = 
            new NetworkCredential(
                options.Value.ServiceAccountUser,
                options.Value.ServiceAccountPassword,
                options.Value.ServiceAccountDomain);
    }
    
    public async Task<string> CreateSalesOrderAsync(
        AxSalesOrderRequest request,
        CancellationToken cancellationToken)
    {
        return await _circuitBreaker.ExecuteAsync(async () =>
        {
            var channel = _channelFactory.CreateChannel();
            
            try
            {
                var response = await channel.CreateSalesOrderAsync(new CreateSalesOrderRequest
                {
                    CustomerAccount = request.CustomerAccount,
                    RequestedDeliveryDate = request.RequestedDeliveryDate,
                    CustomerRef = request.CustomerRef,
                    Lines = request.Lines.Select(l => new SalesLineRequest
                    {
                        ItemId = l.ItemId,
                        Quantity = l.Quantity,
                        UnitPrice = l.UnitPrice ?? 0,
                        WarehouseId = l.WarehouseId ?? "WH-MAIN"
                    }).ToArray()
                });
                
                if (!response.Success)
                {
                    throw new AxException(response.ErrorCode, response.ErrorMessage);
                }
                
                return response.SalesId;
            }
            finally
            {
                ((IClientChannel)channel).Close();
            }
        }, cancellationToken);
    }
}

// WCF Service Contract (generated from AX)
[ServiceContract(Namespace = "http://gbl.com/ax2012/services")]
public interface IGblSalesOrderService
{
    [OperationContract]
    Task<CreateSalesOrderResponse> CreateSalesOrderAsync(CreateSalesOrderRequest request);
    
    [OperationContract]
    Task<UpdateSalesOrderResponse> UpdateSalesOrderAsync(UpdateSalesOrderRequest request);
}
```

### 3.3 Business Connector Client (Admin Operations)

```csharp
namespace GBL.AX2012.MCP.AxConnector.Clients;

public class BusinessConnectorClient : IBusinessConnector, IDisposable
{
    private readonly Axapta _axapta;
    private readonly ILogger<BusinessConnectorClient> _logger;
    private bool _isLoggedOn;
    
    public BusinessConnectorClient(
        IOptions<BusinessConnectorOptions> options,
        ILogger<BusinessConnectorClient> logger)
    {
        _logger = logger;
        _axapta = new Axapta();
        
        try
        {
            _axapta.Logon(
                company: options.Value.Company,
                language: options.Value.Language,
                objectServer: options.Value.ObjectServer,
                configuration: options.Value.Configuration);
            
            _isLoggedOn = true;
            _logger.LogInformation("Business Connector logged on to {Company}", options.Value.Company);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to logon to Business Connector");
            throw;
        }
    }
    
    public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            var result = new HealthCheckResult
            {
                Timestamp = DateTime.UtcNow
            };
            
            try
            {
                // Simple query to verify connectivity
                var stopwatch = Stopwatch.StartNew();
                
                using var record = _axapta.CreateAxaptaRecord("CompanyInfo");
                record.ExecuteStmt("select firstonly * from %1");
                
                result.AosConnected = true;
                result.ResponseTimeMs = stopwatch.ElapsedMilliseconds;
                result.Status = "healthy";
                
                // Check additional components
                result.Details = new Dictionary<string, string>
                {
                    ["database"] = "connected",
                    ["business_connector"] = "connected",
                    ["company"] = record.get_Field("DataAreaId")?.ToString() ?? "unknown"
                };
            }
            catch (Exception ex)
            {
                result.AosConnected = false;
                result.Status = "unhealthy";
                result.Error = ex.Message;
            }
            
            return result;
        }, cancellationToken);
    }
    
    public void Dispose()
    {
        if (_isLoggedOn)
        {
            _axapta.Logoff();
            _isLoggedOn = false;
        }
    }
}
```

---

## 4. Middleware Components

### 4.1 Rate Limiter

```csharp
namespace GBL.AX2012.MCP.Server.Middleware;

public class RateLimiter : IRateLimiter
{
    private readonly ConcurrentDictionary<string, TokenBucket> _buckets = new();
    private readonly RateLimiterOptions _options;
    private readonly ILogger<RateLimiter> _logger;
    
    public RateLimiter(
        IOptions<RateLimiterOptions> options,
        ILogger<RateLimiter> logger)
    {
        _options = options.Value;
        _logger = logger;
    }
    
    public async Task<bool> TryAcquireAsync(string userId, CancellationToken cancellationToken)
    {
        var bucket = _buckets.GetOrAdd(userId, _ => new TokenBucket(
            _options.RequestsPerMinute,
            _options.RequestsPerMinute,
            TimeSpan.FromMinutes(1)));
        
        var acquired = bucket.TryConsume(1);
        
        if (!acquired)
        {
            _logger.LogWarning("Rate limit exceeded for user {UserId}", userId);
        }
        
        return acquired;
    }
    
    private class TokenBucket
    {
        private readonly int _maxTokens;
        private readonly int _refillRate;
        private readonly TimeSpan _refillInterval;
        private double _tokens;
        private DateTime _lastRefill;
        private readonly object _lock = new();
        
        public TokenBucket(int maxTokens, int refillRate, TimeSpan refillInterval)
        {
            _maxTokens = maxTokens;
            _refillRate = refillRate;
            _refillInterval = refillInterval;
            _tokens = maxTokens;
            _lastRefill = DateTime.UtcNow;
        }
        
        public bool TryConsume(int tokens)
        {
            lock (_lock)
            {
                Refill();
                
                if (_tokens >= tokens)
                {
                    _tokens -= tokens;
                    return true;
                }
                
                return false;
            }
        }
        
        private void Refill()
        {
            var now = DateTime.UtcNow;
            var elapsed = now - _lastRefill;
            var tokensToAdd = (elapsed.TotalMilliseconds / _refillInterval.TotalMilliseconds) * _refillRate;
            
            _tokens = Math.Min(_maxTokens, _tokens + tokensToAdd);
            _lastRefill = now;
        }
    }
}
```

### 4.2 Circuit Breaker

```csharp
namespace GBL.AX2012.MCP.Server.Middleware;

public class CircuitBreaker : ICircuitBreaker
{
    private readonly CircuitBreakerOptions _options;
    private readonly ILogger<CircuitBreaker> _logger;
    
    private CircuitState _state = CircuitState.Closed;
    private int _failureCount;
    private DateTime _lastFailure;
    private DateTime _openedAt;
    private readonly object _lock = new();
    
    public CircuitBreaker(
        IOptions<CircuitBreakerOptions> options,
        ILogger<CircuitBreaker> logger)
    {
        _options = options.Value;
        _logger = logger;
    }
    
    public async Task<T> ExecuteAsync<T>(
        Func<Task<T>> action,
        CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            if (_state == CircuitState.Open)
            {
                if (DateTime.UtcNow - _openedAt > _options.OpenDuration)
                {
                    _state = CircuitState.HalfOpen;
                    _logger.LogInformation("Circuit breaker transitioning to half-open");
                }
                else
                {
                    throw new CircuitBreakerOpenException("Circuit breaker is open");
                }
            }
        }
        
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_options.Timeout);
            
            var result = await action();
            
            lock (_lock)
            {
                _failureCount = 0;
                _state = CircuitState.Closed;
            }
            
            return result;
        }
        catch (Exception ex) when (ex is not CircuitBreakerOpenException)
        {
            lock (_lock)
            {
                _failureCount++;
                _lastFailure = DateTime.UtcNow;
                
                if (_failureCount >= _options.FailureThreshold)
                {
                    _state = CircuitState.Open;
                    _openedAt = DateTime.UtcNow;
                    _logger.LogWarning("Circuit breaker opened after {Count} failures", _failureCount);
                }
            }
            
            throw;
        }
    }
    
    private enum CircuitState
    {
        Closed,
        Open,
        HalfOpen
    }
}
```

### 4.3 Idempotency Store

```csharp
namespace GBL.AX2012.MCP.Server.Middleware;

public class IdempotencyStore : IIdempotencyStore
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<IdempotencyStore> _logger;
    
    public IdempotencyStore(
        IDistributedCache cache,
        ILogger<IdempotencyStore> logger)
    {
        _cache = cache;
        _logger = logger;
    }
    
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken) 
        where T : class
    {
        var data = await _cache.GetStringAsync($"idempotency:{key}", cancellationToken);
        
        if (data == null)
        {
            return null;
        }
        
        _logger.LogDebug("Idempotency cache hit for key {Key}", key);
        return JsonSerializer.Deserialize<T>(data);
    }
    
    public async Task SetAsync<T>(
        string key, 
        T value, 
        TimeSpan expiration,
        CancellationToken cancellationToken) 
        where T : class
    {
        var data = JsonSerializer.Serialize(value);
        
        await _cache.SetStringAsync(
            $"idempotency:{key}",
            data,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            },
            cancellationToken);
        
        _logger.LogDebug("Idempotency cache set for key {Key}, expires in {Expiration}", key, expiration);
    }
}
```

---

## 5. Configuration

### 5.1 appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "GBL.AX2012.MCP": "Debug"
    }
  },
  "McpServer": {
    "Transport": "stdio",
    "ServerName": "gbl-ax2012-mcp",
    "ServerVersion": "1.0.0"
  },
  "AifClient": {
    "BaseUrl": "http://ax-aos:8101/DynamicsAx/Services",
    "Timeout": "00:00:30"
  },
  "WcfClient": {
    "BaseUrl": "http://ax-aos:8102/GBL/SalesOrderService.svc",
    "ServiceAccountUser": "svc_mcp",
    "ServiceAccountDomain": "CORP",
    "Timeout": "00:00:30"
  },
  "BusinessConnector": {
    "ObjectServer": "ax-aos:2712",
    "Company": "DAT",
    "Language": "en-us",
    "Configuration": ""
  },
  "RateLimiter": {
    "RequestsPerMinute": 100,
    "Enabled": true
  },
  "CircuitBreaker": {
    "FailureThreshold": 3,
    "OpenDuration": "00:01:00",
    "Timeout": "00:00:30"
  },
  "Audit": {
    "DatabaseConnectionString": "Server=sql-server;Database=MCP_Audit;Integrated Security=true",
    "FileLogPath": "C:\\Logs\\MCP",
    "RetentionDays": 90
  },
  "Security": {
    "RequireAuthentication": true,
    "AllowedRoles": ["MCP_Read", "MCP_Write", "MCP_Admin"],
    "ApprovalThreshold": 50000
  }
}
```

### 5.2 appsettings.Production.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "GBL.AX2012.MCP": "Information"
    }
  },
  "AifClient": {
    "BaseUrl": "http://ax-prod-aos:8101/DynamicsAx/Services"
  },
  "WcfClient": {
    "BaseUrl": "http://ax-prod-aos:8102/GBL/SalesOrderService.svc"
  },
  "BusinessConnector": {
    "ObjectServer": "ax-prod-aos:2712",
    "Company": "PROD"
  },
  "Audit": {
    "DatabaseConnectionString": "Server=sql-prod;Database=MCP_Audit;Integrated Security=true"
  }
}
```

---

## 6. Deployment

### 6.1 Windows Service Installation

```powershell
# install-service.ps1

param(
    [string]$ServiceName = "GBL-AX2012-MCP",
    [string]$DisplayName = "GBL AX 2012 MCP Server",
    [string]$Description = "Model Context Protocol Server for AX 2012 R3",
    [string]$BinaryPath = "C:\Services\GBL-AX2012-MCP\GBL.AX2012.MCP.Server.exe",
    [string]$ServiceAccount = "CORP\svc_mcp"
)

# Stop existing service if running
$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($service) {
    Stop-Service -Name $ServiceName -Force
    sc.exe delete $ServiceName
}

# Create new service
New-Service -Name $ServiceName `
    -DisplayName $DisplayName `
    -Description $Description `
    -BinaryPathName $BinaryPath `
    -StartupType Automatic `
    -Credential (Get-Credential -UserName $ServiceAccount -Message "Enter service account password")

# Configure recovery options
sc.exe failure $ServiceName reset= 86400 actions= restart/60000/restart/60000/restart/60000

# Start service
Start-Service -Name $ServiceName

Write-Host "Service $ServiceName installed and started successfully"
```

### 6.2 Health Check Endpoint

```csharp
// Program.cs - Health endpoint for load balancers

app.MapGet("/health", async (IBusinessConnector bc) =>
{
    var result = await bc.CheckHealthAsync(CancellationToken.None);
    
    return result.Status == "healthy" 
        ? Results.Ok(result) 
        : Results.StatusCode(503);
});

app.MapGet("/health/live", () => Results.Ok(new { status = "alive" }));

app.MapGet("/health/ready", async (IBusinessConnector bc) =>
{
    var result = await bc.CheckHealthAsync(CancellationToken.None);
    return result.AosConnected ? Results.Ok() : Results.StatusCode(503);
});
```

---

## 7. Testing Strategy

### 7.1 Unit Tests

```csharp
namespace GBL.AX2012.MCP.Server.Tests;

public class CreateSalesOrderToolTests
{
    private readonly Mock<IWcfClient> _wcfClientMock;
    private readonly Mock<IAifClient> _aifClientMock;
    private readonly Mock<IIdempotencyStore> _idempotencyStoreMock;
    private readonly CreateSalesOrderTool _sut;
    
    public CreateSalesOrderToolTests()
    {
        _wcfClientMock = new Mock<IWcfClient>();
        _aifClientMock = new Mock<IAifClient>();
        _idempotencyStoreMock = new Mock<IIdempotencyStore>();
        
        _sut = new CreateSalesOrderTool(
            Mock.Of<ILogger<CreateSalesOrderTool>>(),
            Mock.Of<IAuditService>(),
            new CreateSalesOrderInputValidator(),
            _wcfClientMock.Object,
            _aifClientMock.Object,
            _idempotencyStoreMock.Object);
    }
    
    [Fact]
    public async Task ExecuteAsync_ValidInput_CreatesOrder()
    {
        // Arrange
        var input = new CreateSalesOrderInput
        {
            CustomerAccount = "CUST-001",
            Lines = new List<SalesLineInput>
            {
                new() { ItemId = "ITEM-001", Quantity = 10 }
            },
            IdempotencyKey = Guid.NewGuid().ToString()
        };
        
        _aifClientMock.Setup(x => x.GetCustomerAsync("CUST-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Customer { AccountNum = "CUST-001", CreditLimit = 100000, CreditUsed = 0 });
        
        _aifClientMock.Setup(x => x.GetItemAsync("ITEM-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Item { ItemId = "ITEM-001", BlockedForSales = false });
        
        _aifClientMock.Setup(x => x.SimulatePriceAsync("CUST-001", "ITEM-001", 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PriceResult { LineAmount = 1000 });
        
        _wcfClientMock.Setup(x => x.CreateSalesOrderAsync(It.IsAny<AxSalesOrderRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("SO-2025-001234");
        
        // Act
        var result = await _sut.ExecuteCoreAsync(input, new ToolContext { UserId = "test" }, CancellationToken.None);
        
        // Assert
        Assert.True(result.Success);
        Assert.Equal("SO-2025-001234", result.SalesId);
    }
    
    [Fact]
    public async Task ExecuteAsync_CustomerNotFound_ThrowsException()
    {
        // Arrange
        var input = new CreateSalesOrderInput
        {
            CustomerAccount = "INVALID",
            Lines = new List<SalesLineInput>
            {
                new() { ItemId = "ITEM-001", Quantity = 10 }
            },
            IdempotencyKey = Guid.NewGuid().ToString()
        };
        
        _aifClientMock.Setup(x => x.GetCustomerAsync("INVALID", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);
        
        // Act & Assert
        var ex = await Assert.ThrowsAsync<AxException>(() => 
            _sut.ExecuteCoreAsync(input, new ToolContext { UserId = "test" }, CancellationToken.None));
        
        Assert.Equal("CUST_NOT_FOUND", ex.ErrorCode);
    }
    
    [Fact]
    public async Task ExecuteAsync_IdempotencyKeyExists_ReturnsCachedResult()
    {
        // Arrange
        var idempotencyKey = Guid.NewGuid().ToString();
        var cachedResult = new CreateSalesOrderOutput
        {
            Success = true,
            SalesId = "SO-CACHED"
        };
        
        var input = new CreateSalesOrderInput
        {
            CustomerAccount = "CUST-001",
            Lines = new List<SalesLineInput>
            {
                new() { ItemId = "ITEM-001", Quantity = 10 }
            },
            IdempotencyKey = idempotencyKey
        };
        
        _idempotencyStoreMock.Setup(x => x.GetAsync<CreateSalesOrderOutput>(idempotencyKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedResult);
        
        // Act
        var result = await _sut.ExecuteCoreAsync(input, new ToolContext { UserId = "test" }, CancellationToken.None);
        
        // Assert
        Assert.Equal("SO-CACHED", result.SalesId);
        _wcfClientMock.Verify(x => x.CreateSalesOrderAsync(It.IsAny<AxSalesOrderRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
```

### 7.2 Integration Tests

```csharp
namespace GBL.AX2012.MCP.Integration.Tests;

[Collection("AX Integration")]
public class AifClientIntegrationTests : IClassFixture<AifClientFixture>
{
    private readonly IAifClient _client;
    
    public AifClientIntegrationTests(AifClientFixture fixture)
    {
        _client = fixture.Client;
    }
    
    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetCustomerAsync_ExistingCustomer_ReturnsData()
    {
        // Arrange
        var customerAccount = "TEST-CUST-001"; // Known test customer
        
        // Act
        var result = await _client.GetCustomerAsync(customerAccount, CancellationToken.None);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(customerAccount, result.AccountNum);
    }
    
    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetSalesOrderAsync_ExistingOrder_ReturnsData()
    {
        // Arrange
        var salesId = "TEST-SO-001"; // Known test order
        
        // Act
        var result = await _client.GetSalesOrderAsync(salesId, CancellationToken.None);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(salesId, result.SalesId);
        Assert.NotEmpty(result.Lines);
    }
}
```

---

## 8. Monitoring & Observability

### 8.1 Metrics

```csharp
// Prometheus metrics
public static class McpMetrics
{
    public static readonly Counter ToolCallsTotal = Metrics.CreateCounter(
        "mcp_tool_calls_total",
        "Total number of tool calls",
        new CounterConfiguration
        {
            LabelNames = new[] { "tool", "status" }
        });
    
    public static readonly Histogram ToolCallDuration = Metrics.CreateHistogram(
        "mcp_tool_call_duration_seconds",
        "Duration of tool calls in seconds",
        new HistogramConfiguration
        {
            LabelNames = new[] { "tool" },
            Buckets = new[] { 0.1, 0.25, 0.5, 1, 2, 5, 10 }
        });
    
    public static readonly Gauge CircuitBreakerState = Metrics.CreateGauge(
        "mcp_circuit_breaker_state",
        "Circuit breaker state (0=closed, 1=open, 2=half-open)");
    
    public static readonly Counter RateLimitHits = Metrics.CreateCounter(
        "mcp_rate_limit_hits_total",
        "Total number of rate limit hits",
        new CounterConfiguration
        {
            LabelNames = new[] { "user" }
        });
}
```

### 8.2 Structured Logging

```csharp
// Serilog configuration
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .WriteTo.Console(new JsonFormatter())
    .WriteTo.File(
        new JsonFormatter(),
        "logs/mcp-.json",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30)
    .WriteTo.Seq("http://seq-server:5341")
    .CreateLogger();
```

---

## Document History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2025-12-06 | Reinerw | Initial Technical Spec from PRD |

---

## Approval

| Role | Name | Date | Signature |
|------|------|------|-----------|
| Tech Lead | | | |
| Architect | | | |
| Security | | | |
