---
epic: 1
title: "Foundation & Infrastructure"
stories: 8
status: "READY"
project_name: "GBL-AX2012-MCP"
date: "2025-12-06"
---

# Epic 1: Foundation & Infrastructure - Implementation Plans

## Story 1.1: Project Setup & Solution Structure

### Implementation Plan

```
📁 Create Solution Structure
│
├── 1. Create solution file
│   └── dotnet new sln -n GBL.AX2012.MCP
│
├── 2. Create projects
│   ├── dotnet new classlib -n GBL.AX2012.MCP.Core -f net8.0
│   ├── dotnet new classlib -n GBL.AX2012.MCP.AxConnector -f net8.0
│   ├── dotnet new classlib -n GBL.AX2012.MCP.Audit -f net8.0
│   ├── dotnet new console -n GBL.AX2012.MCP.Server -f net8.0
│   ├── dotnet new xunit -n GBL.AX2012.MCP.Server.Tests -f net8.0
│   ├── dotnet new xunit -n GBL.AX2012.MCP.AxConnector.Tests -f net8.0
│   └── dotnet new xunit -n GBL.AX2012.MCP.Integration.Tests -f net8.0
│
├── 3. Add projects to solution
│   └── dotnet sln add src/**/*.csproj tests/**/*.csproj
│
├── 4. Add project references
│   ├── Server → Core, AxConnector, Audit
│   ├── AxConnector → Core
│   └── Audit → Core
│
└── 5. Add NuGet packages
    ├── Core: Microsoft.Extensions.DependencyInjection.Abstractions
    ├── Server: Serilog, Microsoft.Extensions.Hosting
    ├── AxConnector: System.ServiceModel.Http
    └── Audit: Microsoft.EntityFrameworkCore.SqlServer
```

### Files to Create

```csharp
// src/GBL.AX2012.MCP.Core/GBL.AX2012.MCP.Core.csproj
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Options" Version="8.0.0" />
    <PackageReference Include="FluentValidation" Version="11.9.0" />
  </ItemGroup>
</Project>
```

```csharp
// src/GBL.AX2012.MCP.Server/GBL.AX2012.MCP.Server.csproj
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="8.0.0" />
    <PackageReference Include="Serilog.Extensions.Hosting" Version="8.0.0" />
    <PackageReference Include="Serilog.Sinks.Console" Version="5.0.1" />
    <PackageReference Include="Serilog.Sinks.File" Version="5.0.0" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\GBL.AX2012.MCP.Core\GBL.AX2012.MCP.Core.csproj" />
    <ProjectReference Include="..\GBL.AX2012.MCP.AxConnector\GBL.AX2012.MCP.AxConnector.csproj" />
    <ProjectReference Include="..\GBL.AX2012.MCP.Audit\GBL.AX2012.MCP.Audit.csproj" />
  </ItemGroup>
</Project>
```

### Verification

```bash
# Build solution
dotnet build

# Run tests (should pass with 0 tests)
dotnet test

# Verify project structure
tree src/ tests/
```

---

## Story 1.2: Configuration System

### Implementation Plan

```
📁 Configuration System
│
├── 1. Create Options classes in Core
│   ├── McpServerOptions.cs
│   ├── AifClientOptions.cs
│   ├── WcfClientOptions.cs
│   ├── BusinessConnectorOptions.cs
│   ├── RateLimiterOptions.cs
│   ├── CircuitBreakerOptions.cs
│   ├── AuditOptions.cs
│   └── SecurityOptions.cs
│
├── 2. Create appsettings.json in Server
│   └── Full configuration structure
│
├── 3. Create appsettings.Development.json
│   └── Development overrides
│
├── 4. Create configuration validation
│   └── IValidateOptions<T> implementations
│
└── 5. Register configuration in Program.cs
    └── builder.Services.Configure<T>()
```

### Files to Create

```csharp
// src/GBL.AX2012.MCP.Core/Options/McpServerOptions.cs
namespace GBL.AX2012.MCP.Core.Options;

public class McpServerOptions
{
    public const string SectionName = "McpServer";
    
    public string Transport { get; set; } = "stdio";
    public string ServerName { get; set; } = "gbl-ax2012-mcp";
    public string ServerVersion { get; set; } = "1.0.0";
}

// src/GBL.AX2012.MCP.Core/Options/AifClientOptions.cs
namespace GBL.AX2012.MCP.Core.Options;

public class AifClientOptions
{
    public const string SectionName = "AifClient";
    
    public string BaseUrl { get; set; } = "";
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}

// src/GBL.AX2012.MCP.Core/Options/WcfClientOptions.cs
namespace GBL.AX2012.MCP.Core.Options;

public class WcfClientOptions
{
    public const string SectionName = "WcfClient";
    
    public string BaseUrl { get; set; } = "";
    public string ServiceAccountUser { get; set; } = "";
    public string ServiceAccountPassword { get; set; } = "";
    public string ServiceAccountDomain { get; set; } = "";
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}

// src/GBL.AX2012.MCP.Core/Options/RateLimiterOptions.cs
namespace GBL.AX2012.MCP.Core.Options;

public class RateLimiterOptions
{
    public const string SectionName = "RateLimiter";
    
    public int RequestsPerMinute { get; set; } = 100;
    public bool Enabled { get; set; } = true;
}

// src/GBL.AX2012.MCP.Core/Options/CircuitBreakerOptions.cs
namespace GBL.AX2012.MCP.Core.Options;

public class CircuitBreakerOptions
{
    public const string SectionName = "CircuitBreaker";
    
    public int FailureThreshold { get; set; } = 3;
    public TimeSpan OpenDuration { get; set; } = TimeSpan.FromSeconds(60);
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}

// src/GBL.AX2012.MCP.Core/Options/AuditOptions.cs
namespace GBL.AX2012.MCP.Core.Options;

public class AuditOptions
{
    public const string SectionName = "Audit";
    
    public string DatabaseConnectionString { get; set; } = "";
    public string FileLogPath { get; set; } = "";
    public int RetentionDays { get; set; } = 90;
}

// src/GBL.AX2012.MCP.Core/Options/SecurityOptions.cs
namespace GBL.AX2012.MCP.Core.Options;

public class SecurityOptions
{
    public const string SectionName = "Security";
    
    public bool RequireAuthentication { get; set; } = true;
    public string[] AllowedRoles { get; set; } = ["MCP_Read", "MCP_Write", "MCP_Admin"];
    public decimal ApprovalThreshold { get; set; } = 50000m;
}
```

```json
// src/GBL.AX2012.MCP.Server/appsettings.json
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
    "Language": "en-us"
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

### Verification

```csharp
// Test configuration loads correctly
var config = builder.Configuration.GetSection("McpServer").Get<McpServerOptions>();
Assert.NotNull(config);
Assert.Equal("gbl-ax2012-mcp", config.ServerName);
```

---

## Story 1.3: Dependency Injection Setup

### Implementation Plan

```
📁 DI Container Setup
│
├── 1. Create service interfaces in Core
│   ├── IRateLimiter.cs
│   ├── ICircuitBreaker.cs
│   ├── IAuditService.cs
│   ├── IIdempotencyStore.cs
│   └── ITool.cs
│
├── 2. Create ServiceCollectionExtensions
│   └── AddMcpServices() extension method
│
├── 3. Register all services in Program.cs
│   └── builder.Services.AddMcpServices()
│
└── 4. Verify resolution works
    └── Unit test for service resolution
```

### Files to Create

```csharp
// src/GBL.AX2012.MCP.Core/Interfaces/IRateLimiter.cs
namespace GBL.AX2012.MCP.Core.Interfaces;

public interface IRateLimiter
{
    Task<bool> TryAcquireAsync(string userId, CancellationToken cancellationToken = default);
    Task<RateLimitInfo> GetInfoAsync(string userId, CancellationToken cancellationToken = default);
}

public record RateLimitInfo(int Remaining, TimeSpan ResetIn);

// src/GBL.AX2012.MCP.Core/Interfaces/ICircuitBreaker.cs
namespace GBL.AX2012.MCP.Core.Interfaces;

public interface ICircuitBreaker
{
    Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default);
    CircuitState State { get; }
}

public enum CircuitState { Closed, Open, HalfOpen }

// src/GBL.AX2012.MCP.Core/Interfaces/IAuditService.cs
namespace GBL.AX2012.MCP.Core.Interfaces;

public interface IAuditService
{
    Task LogAsync(AuditEntry entry, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditEntry>> QueryAsync(AuditQuery query, CancellationToken cancellationToken = default);
}

// src/GBL.AX2012.MCP.Core/Interfaces/IIdempotencyStore.cs
namespace GBL.AX2012.MCP.Core.Interfaces;

public interface IIdempotencyStore
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken = default) where T : class;
}

// src/GBL.AX2012.MCP.Core/Interfaces/ITool.cs
namespace GBL.AX2012.MCP.Core.Interfaces;

public interface ITool
{
    string Name { get; }
    string Description { get; }
    Task<ToolResponse> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken cancellationToken);
}

// src/GBL.AX2012.MCP.Core/Extensions/ServiceCollectionExtensions.cs
namespace GBL.AX2012.MCP.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMcpServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Options
        services.Configure<McpServerOptions>(configuration.GetSection(McpServerOptions.SectionName));
        services.Configure<AifClientOptions>(configuration.GetSection(AifClientOptions.SectionName));
        services.Configure<WcfClientOptions>(configuration.GetSection(WcfClientOptions.SectionName));
        services.Configure<RateLimiterOptions>(configuration.GetSection(RateLimiterOptions.SectionName));
        services.Configure<CircuitBreakerOptions>(configuration.GetSection(CircuitBreakerOptions.SectionName));
        services.Configure<AuditOptions>(configuration.GetSection(AuditOptions.SectionName));
        services.Configure<SecurityOptions>(configuration.GetSection(SecurityOptions.SectionName));
        
        // Core services
        services.AddSingleton<IRateLimiter, RateLimiter>();
        services.AddSingleton<ICircuitBreaker, CircuitBreaker>();
        services.AddSingleton<IIdempotencyStore, IdempotencyStore>();
        
        // Audit
        services.AddScoped<IAuditService, CompositeAuditService>();
        
        // AX Connectors
        services.AddScoped<IAifClient, AifClient>();
        services.AddScoped<IWcfClient, WcfClient>();
        services.AddSingleton<IBusinessConnector, BusinessConnectorClient>();
        
        // Tools - register all ITool implementations
        services.Scan(scan => scan
            .FromAssemblyOf<HealthCheckTool>()
            .AddClasses(classes => classes.AssignableTo<ITool>())
            .AsImplementedInterfaces()
            .WithScopedLifetime());
        
        return services;
    }
}
```

### Verification

```csharp
[Fact]
public void AddMcpServices_RegistersAllServices()
{
    var services = new ServiceCollection();
    var config = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .Build();
    
    services.AddMcpServices(config);
    var provider = services.BuildServiceProvider();
    
    Assert.NotNull(provider.GetService<IRateLimiter>());
    Assert.NotNull(provider.GetService<ICircuitBreaker>());
    Assert.NotNull(provider.GetService<IAuditService>());
}
```

---

## Story 1.4: MCP Server Host

### Implementation Plan

```
📁 MCP Server Host
│
├── 1. Create McpServer class
│   └── Implements IHostedService
│
├── 2. Create Program.cs entry point
│   └── Configure host and services
│
├── 3. Implement MCP protocol handling
│   └── stdio transport
│
└── 4. Add graceful shutdown
    └── IHostApplicationLifetime
```

### Files to Create

```csharp
// src/GBL.AX2012.MCP.Server/McpServer.cs
namespace GBL.AX2012.MCP.Server;

public class McpServer : IHostedService
{
    private readonly ILogger<McpServer> _logger;
    private readonly IServiceProvider _services;
    private readonly McpServerOptions _options;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly List<ITool> _tools;
    
    public McpServer(
        ILogger<McpServer> logger,
        IServiceProvider services,
        IOptions<McpServerOptions> options,
        IHostApplicationLifetime lifetime,
        IEnumerable<ITool> tools)
    {
        _logger = logger;
        _services = services;
        _options = options.Value;
        _lifetime = lifetime;
        _tools = tools.ToList();
    }
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting MCP Server {Name} v{Version} on {Transport}",
            _options.ServerName, _options.ServerVersion, _options.Transport);
        
        _logger.LogInformation("Registered {Count} tools: {Tools}",
            _tools.Count, string.Join(", ", _tools.Select(t => t.Name)));
        
        // Initialize MCP protocol handler
        await InitializeMcpProtocolAsync(cancellationToken);
    }
    
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping MCP Server");
        await Task.CompletedTask;
    }
    
    private async Task InitializeMcpProtocolAsync(CancellationToken cancellationToken)
    {
        // MCP protocol initialization
        // Read from stdin, write to stdout
        // Handle initialize, tools/list, tools/call messages
        
        using var reader = new StreamReader(Console.OpenStandardInput());
        using var writer = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
        
        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line == null) break;
            
            var response = await ProcessMessageAsync(line, cancellationToken);
            await writer.WriteLineAsync(response);
        }
    }
    
    private async Task<string> ProcessMessageAsync(string message, CancellationToken cancellationToken)
    {
        try
        {
            var request = JsonSerializer.Deserialize<McpRequest>(message);
            
            return request?.Method switch
            {
                "initialize" => HandleInitialize(request),
                "tools/list" => HandleToolsList(),
                "tools/call" => await HandleToolCallAsync(request, cancellationToken),
                _ => CreateErrorResponse(request?.Id, "Unknown method")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message");
            return CreateErrorResponse(null, ex.Message);
        }
    }
    
    private string HandleInitialize(McpRequest request)
    {
        return JsonSerializer.Serialize(new
        {
            jsonrpc = "2.0",
            id = request.Id,
            result = new
            {
                protocolVersion = "2024-11-05",
                capabilities = new { tools = new { } },
                serverInfo = new
                {
                    name = _options.ServerName,
                    version = _options.ServerVersion
                }
            }
        });
    }
    
    private string HandleToolsList()
    {
        var tools = _tools.Select(t => new
        {
            name = t.Name,
            description = t.Description,
            inputSchema = new { type = "object" }
        });
        
        return JsonSerializer.Serialize(new
        {
            jsonrpc = "2.0",
            result = new { tools }
        });
    }
    
    private async Task<string> HandleToolCallAsync(McpRequest request, CancellationToken cancellationToken)
    {
        var toolName = request.Params?.GetProperty("name").GetString();
        var tool = _tools.FirstOrDefault(t => t.Name == toolName);
        
        if (tool == null)
        {
            return CreateErrorResponse(request.Id, $"Tool not found: {toolName}");
        }
        
        var arguments = request.Params?.GetProperty("arguments") ?? default;
        var context = new ToolContext { UserId = "system" }; // TODO: Get from auth
        
        var result = await tool.ExecuteAsync(arguments, context, cancellationToken);
        
        return JsonSerializer.Serialize(new
        {
            jsonrpc = "2.0",
            id = request.Id,
            result = new
            {
                content = new[]
                {
                    new { type = "text", text = JsonSerializer.Serialize(result.Data) }
                }
            }
        });
    }
    
    private string CreateErrorResponse(object? id, string message)
    {
        return JsonSerializer.Serialize(new
        {
            jsonrpc = "2.0",
            id,
            error = new { code = -32603, message }
        });
    }
}

// src/GBL.AX2012.MCP.Server/Program.cs
using GBL.AX2012.MCP.Core.Extensions;
using GBL.AX2012.MCP.Server;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/mcp-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting GBL-AX2012-MCP Server");
    
    var builder = Host.CreateApplicationBuilder(args);
    
    builder.Services.AddSerilog();
    builder.Services.AddMcpServices(builder.Configuration);
    builder.Services.AddHostedService<McpServer>();
    
    var host = builder.Build();
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
```

### Verification

```bash
# Start server
dotnet run --project src/GBL.AX2012.MCP.Server

# Should see:
# Starting MCP Server gbl-ax2012-mcp v1.0.0 on stdio
# Registered X tools: ax_health_check, ...

# Test with MCP initialize message
echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{}}' | dotnet run --project src/GBL.AX2012.MCP.Server
```

---

## Story 1.5: Rate Limiter Implementation

### Implementation Plan

```
📁 Rate Limiter
│
├── 1. Create RateLimiter class
│   └── Token Bucket algorithm
│
├── 2. Create TokenBucket helper
│   └── Thread-safe implementation
│
├── 3. Add unit tests
│   └── Test rate limiting behavior
│
└── 4. Integrate with tool execution
    └── Check before each tool call
```

### Files to Create

```csharp
// src/GBL.AX2012.MCP.Server/Middleware/RateLimiter.cs
namespace GBL.AX2012.MCP.Server.Middleware;

public class RateLimiter : IRateLimiter
{
    private readonly ConcurrentDictionary<string, TokenBucket> _buckets = new();
    private readonly RateLimiterOptions _options;
    private readonly ILogger<RateLimiter> _logger;
    
    public RateLimiter(IOptions<RateLimiterOptions> options, ILogger<RateLimiter> logger)
    {
        _options = options.Value;
        _logger = logger;
    }
    
    public Task<bool> TryAcquireAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
            return Task.FromResult(true);
        
        var bucket = _buckets.GetOrAdd(userId, _ => new TokenBucket(
            _options.RequestsPerMinute,
            _options.RequestsPerMinute,
            TimeSpan.FromMinutes(1)));
        
        var acquired = bucket.TryConsume(1);
        
        if (!acquired)
        {
            _logger.LogWarning("Rate limit exceeded for user {UserId}", userId);
        }
        
        return Task.FromResult(acquired);
    }
    
    public Task<RateLimitInfo> GetInfoAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (_buckets.TryGetValue(userId, out var bucket))
        {
            return Task.FromResult(new RateLimitInfo(bucket.Remaining, bucket.ResetIn));
        }
        
        return Task.FromResult(new RateLimitInfo(_options.RequestsPerMinute, TimeSpan.FromMinutes(1)));
    }
}

// src/GBL.AX2012.MCP.Server/Middleware/TokenBucket.cs
namespace GBL.AX2012.MCP.Server.Middleware;

internal class TokenBucket
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
    
    public int Remaining => (int)_tokens;
    
    public TimeSpan ResetIn
    {
        get
        {
            lock (_lock)
            {
                var elapsed = DateTime.UtcNow - _lastRefill;
                var remaining = _refillInterval - elapsed;
                return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
            }
        }
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
```

### Unit Tests

```csharp
// tests/GBL.AX2012.MCP.Server.Tests/RateLimiterTests.cs
public class RateLimiterTests
{
    [Fact]
    public async Task TryAcquire_UnderLimit_ReturnsTrue()
    {
        var options = Options.Create(new RateLimiterOptions { RequestsPerMinute = 10, Enabled = true });
        var limiter = new RateLimiter(options, Mock.Of<ILogger<RateLimiter>>());
        
        var result = await limiter.TryAcquireAsync("user1");
        
        Assert.True(result);
    }
    
    [Fact]
    public async Task TryAcquire_OverLimit_ReturnsFalse()
    {
        var options = Options.Create(new RateLimiterOptions { RequestsPerMinute = 2, Enabled = true });
        var limiter = new RateLimiter(options, Mock.Of<ILogger<RateLimiter>>());
        
        await limiter.TryAcquireAsync("user1"); // 1
        await limiter.TryAcquireAsync("user1"); // 2
        var result = await limiter.TryAcquireAsync("user1"); // 3 - should fail
        
        Assert.False(result);
    }
    
    [Fact]
    public async Task TryAcquire_Disabled_AlwaysReturnsTrue()
    {
        var options = Options.Create(new RateLimiterOptions { RequestsPerMinute = 1, Enabled = false });
        var limiter = new RateLimiter(options, Mock.Of<ILogger<RateLimiter>>());
        
        for (int i = 0; i < 100; i++)
        {
            Assert.True(await limiter.TryAcquireAsync("user1"));
        }
    }
}
```

---

## Story 1.6: Circuit Breaker Implementation

### Implementation Plan

```
📁 Circuit Breaker
│
├── 1. Create CircuitBreaker class
│   └── State machine implementation
│
├── 2. Create CircuitBreakerOpenException
│   └── Custom exception
│
├── 3. Add unit tests
│   └── Test state transitions
│
└── 4. Integrate with AX clients
    └── Wrap all AX calls
```

### Files to Create

```csharp
// src/GBL.AX2012.MCP.Server/Middleware/CircuitBreaker.cs
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
    
    public CircuitState State => _state;
    
    public CircuitBreaker(IOptions<CircuitBreakerOptions> options, ILogger<CircuitBreaker> logger)
    {
        _options = options.Value;
        _logger = logger;
    }
    
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default)
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
                    throw new CircuitBreakerOpenException(
                        $"Circuit breaker is open. Retry after {(_openedAt + _options.OpenDuration - DateTime.UtcNow).TotalSeconds:F0}s");
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
                if (_state == CircuitState.HalfOpen)
                {
                    _state = CircuitState.Closed;
                    _logger.LogInformation("Circuit breaker closed after successful test");
                }
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
}

// src/GBL.AX2012.MCP.Core/Exceptions/CircuitBreakerOpenException.cs
namespace GBL.AX2012.MCP.Core.Exceptions;

public class CircuitBreakerOpenException : Exception
{
    public CircuitBreakerOpenException(string message) : base(message) { }
}
```

### Unit Tests

```csharp
// tests/GBL.AX2012.MCP.Server.Tests/CircuitBreakerTests.cs
public class CircuitBreakerTests
{
    [Fact]
    public async Task Execute_Success_StaysClosed()
    {
        var options = Options.Create(new CircuitBreakerOptions { FailureThreshold = 3 });
        var cb = new CircuitBreaker(options, Mock.Of<ILogger<CircuitBreaker>>());
        
        var result = await cb.ExecuteAsync(() => Task.FromResult(42));
        
        Assert.Equal(42, result);
        Assert.Equal(CircuitState.Closed, cb.State);
    }
    
    [Fact]
    public async Task Execute_ThreeFailures_Opens()
    {
        var options = Options.Create(new CircuitBreakerOptions 
        { 
            FailureThreshold = 3,
            OpenDuration = TimeSpan.FromMinutes(1)
        });
        var cb = new CircuitBreaker(options, Mock.Of<ILogger<CircuitBreaker>>());
        
        for (int i = 0; i < 3; i++)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                cb.ExecuteAsync<int>(() => throw new InvalidOperationException()));
        }
        
        Assert.Equal(CircuitState.Open, cb.State);
    }
    
    [Fact]
    public async Task Execute_WhenOpen_ThrowsCircuitBreakerOpenException()
    {
        var options = Options.Create(new CircuitBreakerOptions 
        { 
            FailureThreshold = 1,
            OpenDuration = TimeSpan.FromMinutes(1)
        });
        var cb = new CircuitBreaker(options, Mock.Of<ILogger<CircuitBreaker>>());
        
        // Open the circuit
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            cb.ExecuteAsync<int>(() => throw new InvalidOperationException()));
        
        // Should throw CircuitBreakerOpenException
        await Assert.ThrowsAsync<CircuitBreakerOpenException>(() => 
            cb.ExecuteAsync(() => Task.FromResult(42)));
    }
}
```

---

## Story 1.7: Tool Base Class

### Implementation Plan

```
📁 Tool Base Class
│
├── 1. Create ToolBase<TInput, TOutput>
│   └── Generic base class
│
├── 2. Create ToolContext
│   └── Execution context
│
├── 3. Create ToolResponse
│   └── Standard response format
│
└── 4. Add validation integration
    └── FluentValidation
```

### Files to Create

```csharp
// src/GBL.AX2012.MCP.Core/Models/ToolContext.cs
namespace GBL.AX2012.MCP.Core.Models;

public class ToolContext
{
    public string UserId { get; set; } = "anonymous";
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
    public string[] Roles { get; set; } = [];
    public IDictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
}

// src/GBL.AX2012.MCP.Core/Models/ToolResponse.cs
namespace GBL.AX2012.MCP.Core.Models;

public class ToolResponse
{
    public bool Success { get; set; }
    public object? Data { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public long DurationMs { get; set; }
    
    public static ToolResponse Ok(object data) => new() { Success = true, Data = data };
    public static ToolResponse Error(string code, string message) => new() { Success = false, ErrorCode = code, ErrorMessage = message };
}

// src/GBL.AX2012.MCP.Server/Tools/ToolBase.cs
namespace GBL.AX2012.MCP.Server.Tools;

public abstract class ToolBase<TInput, TOutput> : ITool
    where TInput : class
    where TOutput : class
{
    protected readonly ILogger _logger;
    protected readonly IAuditService _audit;
    protected readonly IValidator<TInput>? _validator;
    
    public abstract string Name { get; }
    public abstract string Description { get; }
    
    protected ToolBase(ILogger logger, IAuditService audit, IValidator<TInput>? validator = null)
    {
        _logger = logger;
        _audit = audit;
        _validator = validator;
    }
    
    public async Task<ToolResponse> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var auditEntry = new AuditEntry
        {
            Id = Guid.NewGuid(),
            ToolName = Name,
            UserId = context.UserId,
            CorrelationId = context.CorrelationId,
            Timestamp = DateTime.UtcNow,
            Input = input.ToString()
        };
        
        try
        {
            // Deserialize input
            var typedInput = JsonSerializer.Deserialize<TInput>(input, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            if (typedInput == null)
            {
                throw new ValidationException("Invalid input: could not deserialize");
            }
            
            // Validate input
            if (_validator != null)
            {
                var validationResult = await _validator.ValidateAsync(typedInput, cancellationToken);
                if (!validationResult.IsValid)
                {
                    var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                    throw new ValidationException(errors);
                }
            }
            
            // Execute tool logic
            var output = await ExecuteCoreAsync(typedInput, context, cancellationToken);
            
            auditEntry.Success = true;
            auditEntry.Output = JsonSerializer.Serialize(output);
            auditEntry.DurationMs = stopwatch.ElapsedMilliseconds;
            
            return new ToolResponse
            {
                Success = true,
                Data = output,
                DurationMs = stopwatch.ElapsedMilliseconds
            };
        }
        catch (ValidationException ex)
        {
            auditEntry.Success = false;
            auditEntry.Error = ex.Message;
            auditEntry.DurationMs = stopwatch.ElapsedMilliseconds;
            
            _logger.LogWarning("Validation failed for {Tool}: {Error}", Name, ex.Message);
            
            return ToolResponse.Error("VALIDATION_ERROR", ex.Message);
        }
        catch (AxException ex)
        {
            auditEntry.Success = false;
            auditEntry.Error = ex.Message;
            auditEntry.DurationMs = stopwatch.ElapsedMilliseconds;
            
            _logger.LogError(ex, "AX error in {Tool}", Name);
            
            return ToolResponse.Error(ex.ErrorCode, ex.Message);
        }
        catch (Exception ex)
        {
            auditEntry.Success = false;
            auditEntry.Error = ex.Message;
            auditEntry.DurationMs = stopwatch.ElapsedMilliseconds;
            
            _logger.LogError(ex, "Unexpected error in {Tool}", Name);
            
            return ToolResponse.Error("INTERNAL_ERROR", "An unexpected error occurred");
        }
        finally
        {
            await _audit.LogAsync(auditEntry, cancellationToken);
        }
    }
    
    protected abstract Task<TOutput> ExecuteCoreAsync(TInput input, ToolContext context, CancellationToken cancellationToken);
}
```

---

## Story 1.8: Health Check Tool (Basic)

### Implementation Plan

```
📁 Health Check Tool
│
├── 1. Create HealthCheckInput
│   └── Optional include_details flag
│
├── 2. Create HealthCheckOutput
│   └── Status, timestamp, version
│
├── 3. Create HealthCheckTool
│   └── Extends ToolBase
│
└── 4. Add unit tests
    └── Test health check response
```

### Files to Create

```csharp
// src/GBL.AX2012.MCP.Server/Tools/HealthCheck/HealthCheckInput.cs
namespace GBL.AX2012.MCP.Server.Tools.HealthCheck;

public class HealthCheckInput
{
    public bool IncludeDetails { get; set; } = false;
}

// src/GBL.AX2012.MCP.Server/Tools/HealthCheck/HealthCheckOutput.cs
namespace GBL.AX2012.MCP.Server.Tools.HealthCheck;

public class HealthCheckOutput
{
    public string Status { get; set; } = "healthy";
    public DateTime Timestamp { get; set; }
    public string ServerVersion { get; set; } = "";
    public Dictionary<string, string>? Details { get; set; }
}

// src/GBL.AX2012.MCP.Server/Tools/HealthCheck/HealthCheckTool.cs
namespace GBL.AX2012.MCP.Server.Tools.HealthCheck;

public class HealthCheckTool : ToolBase<HealthCheckInput, HealthCheckOutput>
{
    private readonly McpServerOptions _serverOptions;
    private readonly ICircuitBreaker _circuitBreaker;
    
    public override string Name => "ax_health_check";
    public override string Description => "Check the health status of the MCP Server and its connections";
    
    public HealthCheckTool(
        ILogger<HealthCheckTool> logger,
        IAuditService audit,
        IOptions<McpServerOptions> serverOptions,
        ICircuitBreaker circuitBreaker)
        : base(logger, audit)
    {
        _serverOptions = serverOptions.Value;
        _circuitBreaker = circuitBreaker;
    }
    
    protected override Task<HealthCheckOutput> ExecuteCoreAsync(
        HealthCheckInput input, 
        ToolContext context, 
        CancellationToken cancellationToken)
    {
        var output = new HealthCheckOutput
        {
            Status = "healthy",
            Timestamp = DateTime.UtcNow,
            ServerVersion = _serverOptions.ServerVersion
        };
        
        if (input.IncludeDetails)
        {
            output.Details = new Dictionary<string, string>
            {
                ["server"] = "running",
                ["circuit_breaker"] = _circuitBreaker.State.ToString().ToLower()
            };
        }
        
        return Task.FromResult(output);
    }
}
```

### Unit Tests

```csharp
// tests/GBL.AX2012.MCP.Server.Tests/HealthCheckToolTests.cs
public class HealthCheckToolTests
{
    [Fact]
    public async Task Execute_ReturnsHealthyStatus()
    {
        var tool = CreateTool();
        var input = JsonSerializer.SerializeToElement(new HealthCheckInput());
        var context = new ToolContext { UserId = "test" };
        
        var result = await tool.ExecuteAsync(input, context, CancellationToken.None);
        
        Assert.True(result.Success);
        var output = (HealthCheckOutput)result.Data!;
        Assert.Equal("healthy", output.Status);
    }
    
    [Fact]
    public async Task Execute_WithDetails_IncludesDetails()
    {
        var tool = CreateTool();
        var input = JsonSerializer.SerializeToElement(new HealthCheckInput { IncludeDetails = true });
        var context = new ToolContext { UserId = "test" };
        
        var result = await tool.ExecuteAsync(input, context, CancellationToken.None);
        
        Assert.True(result.Success);
        var output = (HealthCheckOutput)result.Data!;
        Assert.NotNull(output.Details);
        Assert.Equal("running", output.Details["server"]);
    }
    
    private HealthCheckTool CreateTool()
    {
        return new HealthCheckTool(
            Mock.Of<ILogger<HealthCheckTool>>(),
            Mock.Of<IAuditService>(),
            Options.Create(new McpServerOptions { ServerVersion = "1.0.0" }),
            Mock.Of<ICircuitBreaker>());
    }
}
```

---

## Epic 1 Summary

| Story | Files | Tests | Status |
|-------|-------|-------|--------|
| 1.1 | 7 .csproj files | Build verification | Ready |
| 1.2 | 8 Options classes, 2 appsettings | Config load test | Ready |
| 1.3 | 6 interfaces, 1 extension | DI resolution test | Ready |
| 1.4 | McpServer.cs, Program.cs | Startup test | Ready |
| 1.5 | RateLimiter.cs, TokenBucket.cs | 3 unit tests | Ready |
| 1.6 | CircuitBreaker.cs | 3 unit tests | Ready |
| 1.7 | ToolBase.cs, ToolContext.cs, ToolResponse.cs | Integration test | Ready |
| 1.8 | HealthCheckTool.cs | 2 unit tests | Ready |

**Total:** ~25 files, ~15 unit tests
