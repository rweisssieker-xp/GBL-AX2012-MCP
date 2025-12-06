---
epic: 4
title: "Order Creation & Validation"
stories: 7
status: "READY"
project_name: "GBL-AX2012-MCP"
date: "2025-12-06"
---

# Epic 4: Order Creation & Validation - Implementation Plans

## Story 4.1: WCF Client Setup

### Implementation Plan

```
📁 WCF Client
│
├── 1. Create IWcfClient interface
│   └── Define write operations
│
├── 2. Create WCF service contracts
│   └── IGblSalesOrderService
│
├── 3. Create WcfClient implementation
│   └── ChannelFactory with Windows Auth
│
├── 4. Add circuit breaker integration
│   └── Wrap all calls
│
└── 5. Integration test
    └── Test against AX test environment
```

### Files to Create

```csharp
// src/GBL.AX2012.MCP.AxConnector/Interfaces/IWcfClient.cs
namespace GBL.AX2012.MCP.AxConnector.Interfaces;

public interface IWcfClient
{
    Task<string> CreateSalesOrderAsync(AxSalesOrderRequest request, CancellationToken cancellationToken = default);
    Task<bool> UpdateSalesOrderAsync(AxSalesOrderUpdateRequest request, CancellationToken cancellationToken = default);
    Task<bool> ReserveSalesLineAsync(string salesId, int lineNum, decimal quantity, CancellationToken cancellationToken = default);
}

// src/GBL.AX2012.MCP.AxConnector/Contracts/IGblSalesOrderService.cs
namespace GBL.AX2012.MCP.AxConnector.Contracts;

[ServiceContract(Namespace = "http://gbl.com/ax2012/services")]
public interface IGblSalesOrderService
{
    [OperationContract]
    Task<CreateSalesOrderResponse> CreateSalesOrderAsync(CreateSalesOrderRequest request);
    
    [OperationContract]
    Task<UpdateSalesOrderResponse> UpdateSalesOrderAsync(UpdateSalesOrderRequest request);
    
    [OperationContract]
    Task<ReserveSalesLineResponse> ReserveSalesLineAsync(ReserveSalesLineRequest request);
}

[DataContract(Namespace = "http://gbl.com/ax2012/services")]
public class CreateSalesOrderRequest
{
    [DataMember] public string CustomerAccount { get; set; } = "";
    [DataMember] public DateTime RequestedDeliveryDate { get; set; }
    [DataMember] public string? CustomerRef { get; set; }
    [DataMember] public SalesLineRequest[] Lines { get; set; } = [];
}

[DataContract(Namespace = "http://gbl.com/ax2012/services")]
public class SalesLineRequest
{
    [DataMember] public string ItemId { get; set; } = "";
    [DataMember] public decimal Quantity { get; set; }
    [DataMember] public decimal UnitPrice { get; set; }
    [DataMember] public string? WarehouseId { get; set; }
}

[DataContract(Namespace = "http://gbl.com/ax2012/services")]
public class CreateSalesOrderResponse
{
    [DataMember] public bool Success { get; set; }
    [DataMember] public string? SalesId { get; set; }
    [DataMember] public string? ErrorCode { get; set; }
    [DataMember] public string? ErrorMessage { get; set; }
}

// src/GBL.AX2012.MCP.AxConnector/Clients/WcfClient.cs
namespace GBL.AX2012.MCP.AxConnector.Clients;

public class WcfClient : IWcfClient, IDisposable
{
    private readonly ChannelFactory<IGblSalesOrderService> _channelFactory;
    private readonly ILogger<WcfClient> _logger;
    private readonly ICircuitBreaker _circuitBreaker;
    private readonly WcfClientOptions _options;
    
    public WcfClient(
        IOptions<WcfClientOptions> options,
        ILogger<WcfClient> logger,
        ICircuitBreaker circuitBreaker)
    {
        _options = options.Value;
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
            SendTimeout = _options.Timeout,
            ReceiveTimeout = _options.Timeout
        };
        
        var endpoint = new EndpointAddress(_options.BaseUrl);
        _channelFactory = new ChannelFactory<IGblSalesOrderService>(binding, endpoint);
        
        // Use service account credentials
        if (!string.IsNullOrEmpty(_options.ServiceAccountUser))
        {
            _channelFactory.Credentials.Windows.ClientCredential = new NetworkCredential(
                _options.ServiceAccountUser,
                _options.ServiceAccountPassword,
                _options.ServiceAccountDomain);
        }
    }
    
    public async Task<string> CreateSalesOrderAsync(AxSalesOrderRequest request, CancellationToken cancellationToken = default)
    {
        return await _circuitBreaker.ExecuteAsync(async () =>
        {
            var channel = _channelFactory.CreateChannel();
            
            try
            {
                _logger.LogDebug("Creating sales order for customer {Customer}", request.CustomerAccount);
                
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
                    _logger.LogError("AX returned error: {Code} - {Message}", response.ErrorCode, response.ErrorMessage);
                    throw new AxException(response.ErrorCode ?? "AX_ERROR", response.ErrorMessage ?? "Unknown error");
                }
                
                _logger.LogInformation("Created sales order {SalesId}", response.SalesId);
                return response.SalesId!;
            }
            finally
            {
                CloseChannel(channel);
            }
        }, cancellationToken);
    }
    
    public async Task<bool> UpdateSalesOrderAsync(AxSalesOrderUpdateRequest request, CancellationToken cancellationToken = default)
    {
        return await _circuitBreaker.ExecuteAsync(async () =>
        {
            var channel = _channelFactory.CreateChannel();
            
            try
            {
                var response = await channel.UpdateSalesOrderAsync(new UpdateSalesOrderRequest
                {
                    SalesId = request.SalesId,
                    // ... map other fields
                });
                
                return response.Success;
            }
            finally
            {
                CloseChannel(channel);
            }
        }, cancellationToken);
    }
    
    public async Task<bool> ReserveSalesLineAsync(string salesId, int lineNum, decimal quantity, CancellationToken cancellationToken = default)
    {
        return await _circuitBreaker.ExecuteAsync(async () =>
        {
            var channel = _channelFactory.CreateChannel();
            
            try
            {
                var response = await channel.ReserveSalesLineAsync(new ReserveSalesLineRequest
                {
                    SalesId = salesId,
                    LineNum = lineNum,
                    Quantity = quantity
                });
                
                return response.Success;
            }
            finally
            {
                CloseChannel(channel);
            }
        }, cancellationToken);
    }
    
    private void CloseChannel(IGblSalesOrderService channel)
    {
        try
        {
            ((IClientChannel)channel).Close();
        }
        catch
        {
            ((IClientChannel)channel).Abort();
        }
    }
    
    public void Dispose()
    {
        _channelFactory?.Close();
    }
}
```

---

## Story 4.2: Idempotency Store

### Implementation Plan

```
📁 Idempotency Store
│
├── 1. Create IIdempotencyStore interface
│   └── Get, Set operations
│
├── 2. Create MemoryIdempotencyStore
│   └── In-memory for development
│
├── 3. Create SqlIdempotencyStore
│   └── SQL Server for production
│
└── 4. Unit tests
    └── Test idempotency behavior
```

### Files to Create

```csharp
// src/GBL.AX2012.MCP.Server/Middleware/MemoryIdempotencyStore.cs
namespace GBL.AX2012.MCP.Server.Middleware;

public class MemoryIdempotencyStore : IIdempotencyStore
{
    private readonly ConcurrentDictionary<string, CachedResult> _cache = new();
    private readonly ILogger<MemoryIdempotencyStore> _logger;
    
    public MemoryIdempotencyStore(ILogger<MemoryIdempotencyStore> logger)
    {
        _logger = logger;
    }
    
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        if (_cache.TryGetValue(key, out var cached))
        {
            if (cached.ExpiresAt > DateTime.UtcNow)
            {
                _logger.LogDebug("Idempotency cache hit for key {Key}", key);
                return Task.FromResult(JsonSerializer.Deserialize<T>(cached.Data));
            }
            
            // Expired, remove it
            _cache.TryRemove(key, out _);
        }
        
        return Task.FromResult<T?>(null);
    }
    
    public Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken = default) where T : class
    {
        var cached = new CachedResult
        {
            Data = JsonSerializer.Serialize(value),
            ExpiresAt = DateTime.UtcNow + expiration
        };
        
        _cache[key] = cached;
        _logger.LogDebug("Idempotency cache set for key {Key}, expires at {ExpiresAt}", key, cached.ExpiresAt);
        
        return Task.CompletedTask;
    }
    
    private class CachedResult
    {
        public string Data { get; set; } = "";
        public DateTime ExpiresAt { get; set; }
    }
}

// src/GBL.AX2012.MCP.Audit/SqlIdempotencyStore.cs
namespace GBL.AX2012.MCP.Audit;

public class SqlIdempotencyStore : IIdempotencyStore
{
    private readonly AuditDbContext _dbContext;
    private readonly ILogger<SqlIdempotencyStore> _logger;
    
    public SqlIdempotencyStore(AuditDbContext dbContext, ILogger<SqlIdempotencyStore> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }
    
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        var entry = await _dbContext.IdempotencyEntries
            .FirstOrDefaultAsync(e => e.Key == key && e.ExpiresAt > DateTime.UtcNow, cancellationToken);
        
        if (entry != null)
        {
            _logger.LogDebug("Idempotency cache hit for key {Key}", key);
            return JsonSerializer.Deserialize<T>(entry.Data);
        }
        
        return null;
    }
    
    public async Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken = default) where T : class
    {
        var entry = new IdempotencyEntry
        {
            Key = key,
            Data = JsonSerializer.Serialize(value),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow + expiration
        };
        
        _dbContext.IdempotencyEntries.Add(entry);
        await _dbContext.SaveChangesAsync(cancellationToken);
        
        _logger.LogDebug("Idempotency cache set for key {Key}", key);
    }
}

// src/GBL.AX2012.MCP.Audit/Entities/IdempotencyEntry.cs
namespace GBL.AX2012.MCP.Audit.Entities;

public class IdempotencyEntry
{
    public int Id { get; set; }
    public string Key { get; set; } = "";
    public string Data { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
```

### Unit Tests

```csharp
// tests/GBL.AX2012.MCP.Server.Tests/IdempotencyStoreTests.cs
public class IdempotencyStoreTests
{
    [Fact]
    public async Task Get_NonExistent_ReturnsNull()
    {
        var store = new MemoryIdempotencyStore(Mock.Of<ILogger<MemoryIdempotencyStore>>());
        
        var result = await store.GetAsync<TestResult>("non-existent");
        
        Assert.Null(result);
    }
    
    [Fact]
    public async Task SetAndGet_ReturnsStoredValue()
    {
        var store = new MemoryIdempotencyStore(Mock.Of<ILogger<MemoryIdempotencyStore>>());
        var expected = new TestResult { Value = "test" };
        
        await store.SetAsync("key1", expected, TimeSpan.FromMinutes(5));
        var result = await store.GetAsync<TestResult>("key1");
        
        Assert.NotNull(result);
        Assert.Equal("test", result.Value);
    }
    
    [Fact]
    public async Task Get_Expired_ReturnsNull()
    {
        var store = new MemoryIdempotencyStore(Mock.Of<ILogger<MemoryIdempotencyStore>>());
        var expected = new TestResult { Value = "test" };
        
        await store.SetAsync("key1", expected, TimeSpan.FromMilliseconds(1));
        await Task.Delay(10); // Wait for expiration
        var result = await store.GetAsync<TestResult>("key1");
        
        Assert.Null(result);
    }
    
    private class TestResult
    {
        public string Value { get; set; } = "";
    }
}
```

---

## Story 4.3: Create Sales Order - Input Validation

### Files to Create

```csharp
// src/GBL.AX2012.MCP.Server/Tools/CreateSalesOrder/CreateSalesOrderInput.cs
namespace GBL.AX2012.MCP.Server.Tools.CreateSalesOrder;

public class CreateSalesOrderInput
{
    public string CustomerAccount { get; set; } = "";
    public DateTime? RequestedDelivery { get; set; }
    public string? CustomerRef { get; set; }
    public List<SalesLineInput> Lines { get; set; } = new();
    public string IdempotencyKey { get; set; } = "";
}

public class SalesLineInput
{
    public string ItemId { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal? UnitPrice { get; set; }
    public string? Warehouse { get; set; }
}

// src/GBL.AX2012.MCP.Server/Tools/CreateSalesOrder/CreateSalesOrderInputValidator.cs
namespace GBL.AX2012.MCP.Server.Tools.CreateSalesOrder;

public class CreateSalesOrderInputValidator : AbstractValidator<CreateSalesOrderInput>
{
    public CreateSalesOrderInputValidator()
    {
        RuleFor(x => x.CustomerAccount)
            .NotEmpty()
            .WithMessage("customer_account is required")
            .WithErrorCode("INVALID_INPUT");
        
        RuleFor(x => x.Lines)
            .NotEmpty()
            .WithMessage("At least one line is required")
            .WithErrorCode("INVALID_INPUT");
        
        RuleFor(x => x.IdempotencyKey)
            .NotEmpty()
            .WithMessage("idempotency_key is required")
            .WithErrorCode("INVALID_INPUT")
            .Must(BeValidGuid)
            .WithMessage("idempotency_key must be a valid UUID")
            .WithErrorCode("INVALID_INPUT");
        
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ItemId)
                .NotEmpty()
                .WithMessage("item_id is required for each line")
                .WithErrorCode("INVALID_INPUT");
            
            line.RuleFor(l => l.Quantity)
                .GreaterThan(0)
                .WithMessage("quantity must be greater than 0")
                .WithErrorCode("INVALID_QTY");
        });
    }
    
    private bool BeValidGuid(string value)
    {
        return Guid.TryParse(value, out _);
    }
}
```

---

## Story 4.4: Create Sales Order - AX Validation

### Files to Create

```csharp
// src/GBL.AX2012.MCP.Server/Tools/CreateSalesOrder/CreateSalesOrderAxValidator.cs
namespace GBL.AX2012.MCP.Server.Tools.CreateSalesOrder;

public class CreateSalesOrderAxValidator
{
    private readonly IAifClient _aifClient;
    private readonly ILogger<CreateSalesOrderAxValidator> _logger;
    
    public CreateSalesOrderAxValidator(IAifClient aifClient, ILogger<CreateSalesOrderAxValidator> logger)
    {
        _aifClient = aifClient;
        _logger = logger;
    }
    
    public async Task ValidateAsync(CreateSalesOrderInput input, CancellationToken cancellationToken)
    {
        // 1. Validate customer exists
        var customer = await _aifClient.GetCustomerAsync(input.CustomerAccount, cancellationToken);
        if (customer == null)
        {
            throw new AxException("CUST_NOT_FOUND", $"Customer {input.CustomerAccount} not found");
        }
        
        // 2. Validate customer not blocked
        if (customer.Blocked)
        {
            throw new AxException("CUST_BLOCKED", $"Customer {input.CustomerAccount} is blocked for orders");
        }
        
        // 3. Validate all items exist and are not blocked
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
        
        // 4. Calculate order total and check credit
        decimal orderTotal = 0;
        foreach (var line in input.Lines)
        {
            if (line.UnitPrice.HasValue)
            {
                orderTotal += line.UnitPrice.Value * line.Quantity;
            }
            else
            {
                var price = await _aifClient.SimulatePriceAsync(
                    input.CustomerAccount, 
                    line.ItemId, 
                    line.Quantity, 
                    cancellationToken: cancellationToken);
                orderTotal += price.LineAmount;
            }
        }
        
        var creditAvailable = customer.CreditLimit - customer.CreditUsed;
        if (orderTotal > creditAvailable)
        {
            throw new AxException("CREDIT_EXCEEDED", 
                $"Order total {orderTotal:C} exceeds available credit {creditAvailable:C}");
        }
        
        _logger.LogDebug("AX validation passed for order. Total: {Total}, Credit available: {Credit}", 
            orderTotal, creditAvailable);
    }
}
```

---

## Story 4.5: Create Sales Order - Order Creation

### Files to Create

```csharp
// src/GBL.AX2012.MCP.Server/Tools/CreateSalesOrder/CreateSalesOrderOutput.cs
namespace GBL.AX2012.MCP.Server.Tools.CreateSalesOrder;

public class CreateSalesOrderOutput
{
    public bool Success { get; set; }
    public string SalesId { get; set; } = "";
    public string CustomerAccount { get; set; } = "";
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "";
    public int LinesCreated { get; set; }
    public List<string> Warnings { get; set; } = new();
    public string AuditId { get; set; } = "";
    public bool Duplicate { get; set; } = false;
}

// src/GBL.AX2012.MCP.Server/Tools/CreateSalesOrder/CreateSalesOrderTool.cs
namespace GBL.AX2012.MCP.Server.Tools.CreateSalesOrder;

public class CreateSalesOrderTool : ToolBase<CreateSalesOrderInput, CreateSalesOrderOutput>
{
    private readonly IWcfClient _wcfClient;
    private readonly IAifClient _aifClient;
    private readonly IIdempotencyStore _idempotencyStore;
    private readonly CreateSalesOrderAxValidator _axValidator;
    
    public override string Name => "ax_create_salesorder";
    public override string Description => "Create a new sales order in AX 2012";
    
    public CreateSalesOrderTool(
        ILogger<CreateSalesOrderTool> logger,
        IAuditService audit,
        CreateSalesOrderInputValidator validator,
        IWcfClient wcfClient,
        IAifClient aifClient,
        IIdempotencyStore idempotencyStore,
        CreateSalesOrderAxValidator axValidator)
        : base(logger, audit, validator)
    {
        _wcfClient = wcfClient;
        _aifClient = aifClient;
        _idempotencyStore = idempotencyStore;
        _axValidator = axValidator;
    }
    
    protected override async Task<CreateSalesOrderOutput> ExecuteCoreAsync(
        CreateSalesOrderInput input, 
        ToolContext context, 
        CancellationToken cancellationToken)
    {
        // 1. Check idempotency
        var existing = await _idempotencyStore.GetAsync<CreateSalesOrderOutput>(input.IdempotencyKey, cancellationToken);
        if (existing != null)
        {
            _logger.LogInformation("Returning cached result for idempotency key {Key}", input.IdempotencyKey);
            existing.Duplicate = true;
            return existing;
        }
        
        // 2. Validate against AX
        await _axValidator.ValidateAsync(input, cancellationToken);
        
        // 3. Get customer for currency
        var customer = await _aifClient.GetCustomerAsync(input.CustomerAccount, cancellationToken);
        
        // 4. Calculate total
        decimal totalAmount = 0;
        foreach (var line in input.Lines)
        {
            if (line.UnitPrice.HasValue)
            {
                totalAmount += line.UnitPrice.Value * line.Quantity;
            }
            else
            {
                var price = await _aifClient.SimulatePriceAsync(
                    input.CustomerAccount, line.ItemId, line.Quantity, cancellationToken: cancellationToken);
                totalAmount += price.LineAmount;
            }
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
            TotalAmount = totalAmount,
            Currency = customer?.Currency ?? "EUR",
            LinesCreated = input.Lines.Count,
            Warnings = new List<string>(),
            AuditId = Guid.NewGuid().ToString()
        };
        
        // 7. Store for idempotency
        await _idempotencyStore.SetAsync(input.IdempotencyKey, output, TimeSpan.FromDays(7), cancellationToken);
        
        _logger.LogInformation("Created sales order {SalesId} for customer {Customer}", salesId, input.CustomerAccount);
        
        return output;
    }
}
```

---

## Story 4.6: Create Sales Order - Error Handling

Error handling is built into the `ToolBase` class and `CreateSalesOrderTool`. Additional error mapping:

```csharp
// src/GBL.AX2012.MCP.Core/Exceptions/AxException.cs
namespace GBL.AX2012.MCP.Core.Exceptions;

public class AxException : Exception
{
    public string ErrorCode { get; }
    
    public AxException(string errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }
    
    public static AxException FromAxError(string? axErrorCode, string? axMessage)
    {
        var (code, message) = MapAxError(axErrorCode, axMessage);
        return new AxException(code, message);
    }
    
    private static (string code, string message) MapAxError(string? axCode, string? axMessage)
    {
        return axCode switch
        {
            "TIMEOUT" => ("AX_TIMEOUT", "System temporarily unavailable"),
            "VALIDATION" => ("AX_VALIDATION", axMessage ?? "Validation error"),
            _ => ("AX_ERROR", axMessage ?? "System error")
        };
    }
}
```

---

## Story 4.7: Create Sales Order - Audit Trail

Audit logging is built into the `ToolBase` class. The `CreateSalesOrderTool` automatically logs:

- Input parameters (customer, lines, idempotency key)
- Output (sales ID, total, audit ID)
- Success/failure status
- Duration
- User ID and correlation ID

Additional audit entry for write operations is stored in the database via `DatabaseAuditService`.

---

## Epic 4 Summary

| Story | Files | Tests | Status |
|-------|-------|-------|--------|
| 4.1 | IWcfClient, WcfClient, Contracts | Integration test | Ready |
| 4.2 | MemoryIdempotencyStore, SqlIdempotencyStore | 3 unit tests | Ready |
| 4.3 | CreateSalesOrderInput, Validator | 3 unit tests | Ready |
| 4.4 | CreateSalesOrderAxValidator | 4 unit tests | Ready |
| 4.5 | CreateSalesOrderTool, Output | 3 unit tests | Ready |
| 4.6 | AxException (enhanced) | 2 unit tests | Ready |
| 4.7 | (Built into ToolBase) | 1 unit test | Ready |

**Total:** ~15 files, ~16 unit tests
