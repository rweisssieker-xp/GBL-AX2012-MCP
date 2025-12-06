---
epic: 6
title: "Resilience & Monitoring"
stories: 4
status: "READY"
project_name: "GBL-AX2012-MCP"
date: "2025-12-06"
---

# Epic 6: Resilience & Monitoring - Implementation Plans

## Story 6.1: Full Health Check

### Implementation Plan

```
📁 Full Health Check
│
├── 1. Create IBusinessConnector interface
│   └── AX connectivity check
│
├── 2. Create BusinessConnectorClient
│   └── BC.NET implementation
│
├── 3. Extend HealthCheckTool
│   └── Full component status
│
└── 4. Integration test
    └── Test against AX
```

### Files to Create

```csharp
// src/GBL.AX2012.MCP.AxConnector/Interfaces/IBusinessConnector.cs
namespace GBL.AX2012.MCP.AxConnector.Interfaces;

public interface IBusinessConnector : IDisposable
{
    Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default);
    bool IsConnected { get; }
}

public class HealthCheckResult
{
    public string Status { get; set; } = "unknown";
    public bool AosConnected { get; set; }
    public long ResponseTimeMs { get; set; }
    public DateTime Timestamp { get; set; }
    public Dictionary<string, string>? Details { get; set; }
    public string? Error { get; set; }
}

// src/GBL.AX2012.MCP.AxConnector/Clients/BusinessConnectorClient.cs
namespace GBL.AX2012.MCP.AxConnector.Clients;

public class BusinessConnectorClient : IBusinessConnector
{
    private readonly ILogger<BusinessConnectorClient> _logger;
    private readonly BusinessConnectorOptions _options;
    private object? _axapta; // Dynamic to avoid compile-time dependency
    private bool _isLoggedOn;
    private readonly object _lock = new();
    
    public bool IsConnected => _isLoggedOn;
    
    public BusinessConnectorClient(
        IOptions<BusinessConnectorOptions> options,
        ILogger<BusinessConnectorClient> logger)
    {
        _options = options.Value;
        _logger = logger;
    }
    
    public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var result = new HealthCheckResult
            {
                Timestamp = DateTime.UtcNow
            };
            
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                EnsureConnected();
                
                // Execute a simple query to verify connectivity
                var companyInfo = ExecuteQuery("select firstonly DataAreaId from CompanyInfo");
                
                result.AosConnected = true;
                result.ResponseTimeMs = stopwatch.ElapsedMilliseconds;
                result.Status = "healthy";
                result.Details = new Dictionary<string, string>
                {
                    ["database"] = "connected",
                    ["business_connector"] = "connected",
                    ["company"] = companyInfo ?? _options.Company,
                    ["aos"] = _options.ObjectServer
                };
            }
            catch (Exception ex)
            {
                result.AosConnected = false;
                result.ResponseTimeMs = stopwatch.ElapsedMilliseconds;
                result.Status = "unhealthy";
                result.Error = ex.Message;
                result.Details = new Dictionary<string, string>
                {
                    ["database"] = "unknown",
                    ["business_connector"] = "error",
                    ["error_type"] = ex.GetType().Name
                };
                
                _logger.LogError(ex, "Health check failed");
            }
            
            return result;
        }, cancellationToken);
    }
    
    private void EnsureConnected()
    {
        lock (_lock)
        {
            if (_isLoggedOn) return;
            
            try
            {
                // Use reflection to load BC.NET dynamically
                var axaptaType = Type.GetType("Microsoft.Dynamics.BusinessConnectorNet.Axapta, Microsoft.Dynamics.BusinessConnectorNet");
                
                if (axaptaType == null)
                {
                    throw new InvalidOperationException("Business Connector .NET is not installed");
                }
                
                _axapta = Activator.CreateInstance(axaptaType);
                
                var logonMethod = axaptaType.GetMethod("Logon");
                logonMethod?.Invoke(_axapta, new object?[]
                {
                    _options.Company,
                    _options.Language,
                    _options.ObjectServer,
                    _options.Configuration
                });
                
                _isLoggedOn = true;
                _logger.LogInformation("Business Connector logged on to {Company} at {AOS}", 
                    _options.Company, _options.ObjectServer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to logon to Business Connector");
                throw;
            }
        }
    }
    
    private string? ExecuteQuery(string statement)
    {
        if (_axapta == null) return null;
        
        var axaptaType = _axapta.GetType();
        var createRecordMethod = axaptaType.GetMethod("CreateAxaptaRecord");
        
        var record = createRecordMethod?.Invoke(_axapta, new object[] { "CompanyInfo" });
        if (record == null) return null;
        
        var recordType = record.GetType();
        var executeMethod = recordType.GetMethod("ExecuteStmt");
        executeMethod?.Invoke(record, new object[] { statement });
        
        var getFieldMethod = recordType.GetMethod("get_Field");
        var result = getFieldMethod?.Invoke(record, new object[] { "DataAreaId" });
        
        return result?.ToString();
    }
    
    public void Dispose()
    {
        lock (_lock)
        {
            if (_isLoggedOn && _axapta != null)
            {
                try
                {
                    var logoffMethod = _axapta.GetType().GetMethod("Logoff");
                    logoffMethod?.Invoke(_axapta, null);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error during Business Connector logoff");
                }
                
                _isLoggedOn = false;
            }
        }
    }
}

// Updated HealthCheckTool for full health check
// src/GBL.AX2012.MCP.Server/Tools/HealthCheck/HealthCheckTool.cs (updated)
namespace GBL.AX2012.MCP.Server.Tools.HealthCheck;

public class HealthCheckTool : ToolBase<HealthCheckInput, HealthCheckOutput>
{
    private readonly McpServerOptions _serverOptions;
    private readonly ICircuitBreaker _circuitBreaker;
    private readonly IRateLimiter _rateLimiter;
    private readonly IBusinessConnector _businessConnector;
    private readonly IAifClient _aifClient;
    
    public override string Name => "ax_health_check";
    public override string Description => "Check the health status of the MCP Server and AX 2012 connections";
    
    public HealthCheckTool(
        ILogger<HealthCheckTool> logger,
        IAuditService audit,
        IOptions<McpServerOptions> serverOptions,
        ICircuitBreaker circuitBreaker,
        IRateLimiter rateLimiter,
        IBusinessConnector businessConnector,
        IAifClient aifClient)
        : base(logger, audit)
    {
        _serverOptions = serverOptions.Value;
        _circuitBreaker = circuitBreaker;
        _rateLimiter = rateLimiter;
        _businessConnector = businessConnector;
        _aifClient = aifClient;
    }
    
    protected override async Task<HealthCheckOutput> ExecuteCoreAsync(
        HealthCheckInput input, 
        ToolContext context, 
        CancellationToken cancellationToken)
    {
        var output = new HealthCheckOutput
        {
            Timestamp = DateTime.UtcNow,
            ServerVersion = _serverOptions.ServerVersion
        };
        
        if (!input.IncludeDetails)
        {
            // Basic health check - just server status
            output.Status = "healthy";
            return output;
        }
        
        // Full health check with AX connectivity
        var bcHealth = await _businessConnector.CheckHealthAsync(cancellationToken);
        
        output.AosConnected = bcHealth.AosConnected;
        output.ResponseTimeMs = bcHealth.ResponseTimeMs;
        
        // Determine overall status
        var issues = new List<string>();
        
        if (!bcHealth.AosConnected)
        {
            issues.Add("AOS not connected");
        }
        
        if (_circuitBreaker.State == CircuitState.Open)
        {
            issues.Add("Circuit breaker open");
        }
        
        output.Status = issues.Count switch
        {
            0 => "healthy",
            1 => "degraded",
            _ => "unhealthy"
        };
        
        output.Details = new Dictionary<string, string>
        {
            ["server"] = "running",
            ["server_version"] = _serverOptions.ServerVersion,
            ["circuit_breaker"] = _circuitBreaker.State.ToString().ToLower(),
            ["rate_limiter"] = "enabled",
            ["business_connector"] = bcHealth.AosConnected ? "connected" : "disconnected",
            ["aos"] = bcHealth.Details?.GetValueOrDefault("aos") ?? "unknown",
            ["company"] = bcHealth.Details?.GetValueOrDefault("company") ?? "unknown"
        };
        
        if (bcHealth.Error != null)
        {
            output.Details["error"] = bcHealth.Error;
        }
        
        return output;
    }
}
```

---

## Story 6.2: HTTP Health Endpoints

### Implementation Plan

```
📁 HTTP Health Endpoints
│
├── 1. Add ASP.NET Core minimal API
│   └── Health endpoints
│
├── 2. Create health check services
│   └── IHealthCheck implementations
│
├── 3. Configure endpoints
│   └── /health, /health/live, /health/ready
│
└── 4. Integration test
    └── Test endpoints
```

### Files to Create

```csharp
// src/GBL.AX2012.MCP.Server/Health/AxHealthCheck.cs
namespace GBL.AX2012.MCP.Server.Health;

public class AxHealthCheck : IHealthCheck
{
    private readonly IBusinessConnector _businessConnector;
    
    public AxHealthCheck(IBusinessConnector businessConnector)
    {
        _businessConnector = businessConnector;
    }
    
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _businessConnector.CheckHealthAsync(cancellationToken);
            
            if (result.AosConnected)
            {
                return HealthCheckResult.Healthy($"AOS connected in {result.ResponseTimeMs}ms", 
                    new Dictionary<string, object>
                    {
                        ["response_time_ms"] = result.ResponseTimeMs,
                        ["company"] = result.Details?.GetValueOrDefault("company") ?? "unknown"
                    });
            }
            
            return HealthCheckResult.Unhealthy($"AOS not connected: {result.Error}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"Health check failed: {ex.Message}");
        }
    }
}

// src/GBL.AX2012.MCP.Server/Health/CircuitBreakerHealthCheck.cs
namespace GBL.AX2012.MCP.Server.Health;

public class CircuitBreakerHealthCheck : IHealthCheck
{
    private readonly ICircuitBreaker _circuitBreaker;
    
    public CircuitBreakerHealthCheck(ICircuitBreaker circuitBreaker)
    {
        _circuitBreaker = circuitBreaker;
    }
    
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        var state = _circuitBreaker.State;
        
        return Task.FromResult(state switch
        {
            CircuitState.Closed => HealthCheckResult.Healthy("Circuit breaker closed"),
            CircuitState.HalfOpen => HealthCheckResult.Degraded("Circuit breaker half-open"),
            CircuitState.Open => HealthCheckResult.Unhealthy("Circuit breaker open"),
            _ => HealthCheckResult.Unhealthy("Unknown circuit breaker state")
        });
    }
}

// Update Program.cs to add health endpoints
// src/GBL.AX2012.MCP.Server/Program.cs (additions)

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck<AxHealthCheck>("ax_connectivity", tags: new[] { "ready" })
    .AddCheck<CircuitBreakerHealthCheck>("circuit_breaker", tags: new[] { "ready" });

// Build app
var app = builder.Build();

// Map health endpoints
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false, // No checks - just confirms app is running
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = "alive" }));
    }
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = new
        {
            status = report.Status.ToString().ToLower(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString().ToLower(),
                description = e.Value.Description
            })
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(result));
    }
});

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = new
        {
            status = report.Status.ToString().ToLower(),
            timestamp = DateTime.UtcNow,
            duration_ms = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString().ToLower(),
                description = e.Value.Description,
                duration_ms = e.Value.Duration.TotalMilliseconds,
                data = e.Value.Data
            })
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(result, new JsonSerializerOptions
        {
            WriteIndented = true
        }));
    }
});
```

---

## Story 6.3: Prometheus Metrics

### Implementation Plan

```
📁 Prometheus Metrics
│
├── 1. Add prometheus-net package
│   └── NuGet reference
│
├── 2. Create McpMetrics class
│   └── Define all metrics
│
├── 3. Instrument tool execution
│   └── Record metrics in ToolBase
│
├── 4. Expose /metrics endpoint
│   └── Prometheus format
│
└── 5. Create Grafana dashboard
    └── JSON template
```

### Files to Create

```csharp
// src/GBL.AX2012.MCP.Server/Metrics/McpMetrics.cs
namespace GBL.AX2012.MCP.Server.Metrics;

public static class McpMetrics
{
    private static readonly Counter _toolCallsTotal = Prometheus.Metrics.CreateCounter(
        "mcp_tool_calls_total",
        "Total number of MCP tool calls",
        new CounterConfiguration
        {
            LabelNames = new[] { "tool", "status" }
        });
    
    private static readonly Histogram _toolCallDuration = Prometheus.Metrics.CreateHistogram(
        "mcp_tool_call_duration_seconds",
        "Duration of MCP tool calls in seconds",
        new HistogramConfiguration
        {
            LabelNames = new[] { "tool" },
            Buckets = new[] { 0.05, 0.1, 0.25, 0.5, 1, 2, 5, 10 }
        });
    
    private static readonly Gauge _circuitBreakerState = Prometheus.Metrics.CreateGauge(
        "mcp_circuit_breaker_state",
        "Circuit breaker state (0=closed, 1=open, 2=half-open)");
    
    private static readonly Counter _rateLimitHits = Prometheus.Metrics.CreateCounter(
        "mcp_rate_limit_hits_total",
        "Total number of rate limit hits",
        new CounterConfiguration
        {
            LabelNames = new[] { "user" }
        });
    
    private static readonly Gauge _activeConnections = Prometheus.Metrics.CreateGauge(
        "mcp_active_connections",
        "Number of active MCP connections");
    
    private static readonly Counter _axCallsTotal = Prometheus.Metrics.CreateCounter(
        "mcp_ax_calls_total",
        "Total number of AX service calls",
        new CounterConfiguration
        {
            LabelNames = new[] { "service", "operation", "status" }
        });
    
    private static readonly Histogram _axCallDuration = Prometheus.Metrics.CreateHistogram(
        "mcp_ax_call_duration_seconds",
        "Duration of AX service calls in seconds",
        new HistogramConfiguration
        {
            LabelNames = new[] { "service", "operation" },
            Buckets = new[] { 0.1, 0.25, 0.5, 1, 2, 5, 10, 30 }
        });
    
    public static void RecordToolCall(string tool, bool success, double durationSeconds)
    {
        _toolCallsTotal.WithLabels(tool, success ? "success" : "error").Inc();
        _toolCallDuration.WithLabels(tool).Observe(durationSeconds);
    }
    
    public static void RecordRateLimitHit(string user)
    {
        _rateLimitHits.WithLabels(user).Inc();
    }
    
    public static void SetCircuitBreakerState(CircuitState state)
    {
        _circuitBreakerState.Set(state switch
        {
            CircuitState.Closed => 0,
            CircuitState.Open => 1,
            CircuitState.HalfOpen => 2,
            _ => -1
        });
    }
    
    public static void IncrementActiveConnections() => _activeConnections.Inc();
    public static void DecrementActiveConnections() => _activeConnections.Dec();
    
    public static void RecordAxCall(string service, string operation, bool success, double durationSeconds)
    {
        _axCallsTotal.WithLabels(service, operation, success ? "success" : "error").Inc();
        _axCallDuration.WithLabels(service, operation).Observe(durationSeconds);
    }
}

// Update ToolBase to record metrics
// In ToolBase.ExecuteAsync, add:
finally
{
    McpMetrics.RecordToolCall(Name, auditEntry.Success, stopwatch.Elapsed.TotalSeconds);
    await _audit.LogAsync(auditEntry, cancellationToken);
}

// Update Program.cs to expose metrics endpoint
app.UseMetricServer(); // Exposes /metrics

// Or for more control:
app.MapGet("/metrics", async context =>
{
    context.Response.ContentType = "text/plain; version=0.0.4; charset=utf-8";
    await using var stream = context.Response.Body;
    await Prometheus.Metrics.DefaultRegistry.CollectAndExportAsTextAsync(stream);
});
```

### Grafana Dashboard Template

```json
// docs/monitoring/grafana-dashboard.json
{
  "title": "GBL-AX2012-MCP Dashboard",
  "panels": [
    {
      "title": "Tool Calls per Second",
      "type": "graph",
      "targets": [
        {
          "expr": "rate(mcp_tool_calls_total[5m])",
          "legendFormat": "{{tool}} - {{status}}"
        }
      ]
    },
    {
      "title": "Tool Call Duration (p95)",
      "type": "graph",
      "targets": [
        {
          "expr": "histogram_quantile(0.95, rate(mcp_tool_call_duration_seconds_bucket[5m]))",
          "legendFormat": "{{tool}}"
        }
      ]
    },
    {
      "title": "Circuit Breaker State",
      "type": "stat",
      "targets": [
        {
          "expr": "mcp_circuit_breaker_state",
          "legendFormat": "State"
        }
      ],
      "mappings": [
        { "value": 0, "text": "CLOSED", "color": "green" },
        { "value": 1, "text": "OPEN", "color": "red" },
        { "value": 2, "text": "HALF-OPEN", "color": "yellow" }
      ]
    },
    {
      "title": "Rate Limit Hits",
      "type": "graph",
      "targets": [
        {
          "expr": "rate(mcp_rate_limit_hits_total[5m])",
          "legendFormat": "{{user}}"
        }
      ]
    },
    {
      "title": "AX Service Latency (p95)",
      "type": "graph",
      "targets": [
        {
          "expr": "histogram_quantile(0.95, rate(mcp_ax_call_duration_seconds_bucket[5m]))",
          "legendFormat": "{{service}}/{{operation}}"
        }
      ]
    },
    {
      "title": "Error Rate",
      "type": "stat",
      "targets": [
        {
          "expr": "sum(rate(mcp_tool_calls_total{status=\"error\"}[5m])) / sum(rate(mcp_tool_calls_total[5m])) * 100",
          "legendFormat": "Error %"
        }
      ],
      "thresholds": [
        { "value": 0, "color": "green" },
        { "value": 2, "color": "yellow" },
        { "value": 5, "color": "red" }
      ]
    }
  ]
}
```

---

## Story 6.4: Structured Logging

### Implementation Plan

```
📁 Structured Logging
│
├── 1. Configure Serilog
│   └── JSON formatter
│
├── 2. Add enrichers
│   └── Machine, environment, correlation
│
├── 3. Configure sinks
│   └── Console, file, Seq
│
└── 4. Add log scopes
    └── Correlation ID, user ID
```

### Files to Create

```csharp
// src/GBL.AX2012.MCP.Server/Logging/LoggingConfiguration.cs
namespace GBL.AX2012.MCP.Server.Logging;

public static class LoggingConfiguration
{
    public static IHostBuilder ConfigureLogging(this IHostBuilder builder)
    {
        return builder.UseSerilog((context, services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithEnvironmentName()
                .Enrich.WithProperty("Application", "GBL-AX2012-MCP")
                .Enrich.WithProperty("Version", typeof(LoggingConfiguration).Assembly.GetName().Version?.ToString() ?? "1.0.0")
                .WriteTo.Console(new JsonFormatter())
                .WriteTo.File(
                    new JsonFormatter(),
                    path: "logs/mcp-.json",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    fileSizeLimitBytes: 100 * 1024 * 1024) // 100 MB
                .WriteTo.Conditional(
                    evt => !string.IsNullOrEmpty(context.Configuration["Seq:ServerUrl"]),
                    wt => wt.Seq(context.Configuration["Seq:ServerUrl"] ?? "http://localhost:5341"));
        });
    }
}

// src/GBL.AX2012.MCP.Server/Logging/CorrelationIdEnricher.cs
namespace GBL.AX2012.MCP.Server.Logging;

public class CorrelationIdEnricher : ILogEventEnricher
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public CorrelationIdEnricher(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var correlationId = _httpContextAccessor.HttpContext?.Items["CorrelationId"]?.ToString()
            ?? Activity.Current?.Id
            ?? Guid.NewGuid().ToString();
        
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("CorrelationId", correlationId));
    }
}

// src/GBL.AX2012.MCP.Server/Logging/LoggingMiddleware.cs
namespace GBL.AX2012.MCP.Server.Logging;

public class LoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LoggingMiddleware> _logger;
    
    public LoggingMiddleware(RequestDelegate next, ILogger<LoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
            ?? Guid.NewGuid().ToString();
        
        context.Items["CorrelationId"] = correlationId;
        context.Response.Headers["X-Correlation-ID"] = correlationId;
        
        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("RequestPath", context.Request.Path))
        using (LogContext.PushProperty("RequestMethod", context.Request.Method))
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                await _next(context);
                
                _logger.LogInformation(
                    "HTTP {Method} {Path} responded {StatusCode} in {Duration}ms",
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode,
                    stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "HTTP {Method} {Path} failed after {Duration}ms",
                    context.Request.Method,
                    context.Request.Path,
                    stopwatch.ElapsedMilliseconds);
                
                throw;
            }
        }
    }
}

// Update Program.cs
var builder = Host.CreateApplicationBuilder(args);
builder.ConfigureLogging();

// In tool execution, use log scopes:
using (_logger.BeginScope(new Dictionary<string, object>
{
    ["ToolName"] = Name,
    ["UserId"] = context.UserId,
    ["CorrelationId"] = context.CorrelationId
}))
{
    // ... tool execution
}
```

### appsettings.json Logging Configuration

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.Hosting.Lifetime": "Information",
        "System": "Warning",
        "GBL.AX2012.MCP": "Debug"
      }
    },
    "Enrich": ["FromLogContext", "WithMachineName", "WithEnvironmentName"],
    "Properties": {
      "Application": "GBL-AX2012-MCP"
    }
  },
  "Seq": {
    "ServerUrl": "http://seq-server:5341",
    "ApiKey": ""
  }
}
```

### Example Log Output

```json
{
  "Timestamp": "2025-12-06T14:30:00.123Z",
  "Level": "Information",
  "MessageTemplate": "Tool {ToolName} completed in {Duration}ms",
  "Properties": {
    "ToolName": "ax_create_salesorder",
    "Duration": 1234,
    "UserId": "CORP\\jsmith",
    "CorrelationId": "abc-123-def",
    "MachineName": "MCP-SERVER-01",
    "Environment": "Production",
    "Application": "GBL-AX2012-MCP",
    "Version": "1.0.0"
  }
}
```

---

## Epic 6 Summary

| Story | Files | Tests | Status |
|-------|-------|-------|--------|
| 6.1 | BusinessConnectorClient, HealthCheckTool (updated) | 2 integration tests | Ready |
| 6.2 | AxHealthCheck, CircuitBreakerHealthCheck, endpoints | 3 unit tests | Ready |
| 6.3 | McpMetrics, Grafana dashboard | 2 unit tests | Ready |
| 6.4 | LoggingConfiguration, CorrelationIdEnricher | 1 unit test | Ready |

**Total:** ~12 files, ~8 unit tests

---

## All Epics Complete Summary

| Epic | Stories | Files | Tests |
|------|---------|-------|-------|
| 1 - Foundation | 8 | ~25 | ~15 |
| 2 - Read Operations | 6 | ~15 | ~12 |
| 3 - Inventory & Pricing | 4 | ~10 | ~7 |
| 4 - Order Creation | 7 | ~15 | ~16 |
| 5 - Security & Audit | 5 | ~15 | ~12 |
| 6 - Resilience & Monitoring | 4 | ~12 | ~8 |
| **TOTAL** | **34** | **~92** | **~70** |
