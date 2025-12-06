using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using GBL.AX2012.MCP.Core.Interfaces;
using GBL.AX2012.MCP.Core.Models;
using GBL.AX2012.MCP.Core.Options;
using GBL.AX2012.MCP.AxConnector.Interfaces;

namespace GBL.AX2012.MCP.Server.Tools;

public class HealthCheckInput
{
    public bool IncludeDetails { get; set; } = false;
}

public class HealthCheckOutput
{
    public string Status { get; set; } = "healthy";
    public DateTime Timestamp { get; set; }
    public string ServerVersion { get; set; } = "";
    public bool AosConnected { get; set; }
    public long ResponseTimeMs { get; set; }
    public Dictionary<string, string>? Details { get; set; }
}

public class HealthCheckTool : ToolBase<HealthCheckInput, HealthCheckOutput>
{
    private readonly McpServerOptions _serverOptions;
    private readonly ICircuitBreaker _circuitBreaker;
    private readonly IBusinessConnector _businessConnector;
    
    public override string Name => "ax_health_check";
    public override string Description => "Check the health status of the MCP Server and AX 2012 connections";
    
    public HealthCheckTool(
        ILogger<HealthCheckTool> logger,
        IAuditService audit,
        IOptions<McpServerOptions> serverOptions,
        ICircuitBreaker circuitBreaker,
        IBusinessConnector businessConnector)
        : base(logger, audit)
    {
        _serverOptions = serverOptions.Value;
        _circuitBreaker = circuitBreaker;
        _businessConnector = businessConnector;
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
            output.Status = "healthy";
            output.AosConnected = true;
            return output;
        }
        
        // Full health check with AX connectivity
        var bcHealth = await _businessConnector.CheckHealthAsync(cancellationToken);
        
        output.AosConnected = bcHealth.AosConnected;
        output.ResponseTimeMs = bcHealth.ResponseTimeMs;
        
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
            ["business_connector"] = bcHealth.AosConnected ? "connected" : "disconnected"
        };
        
        foreach (var detail in bcHealth.Details)
        {
            output.Details[detail.Key] = detail.Value;
        }
        
        if (bcHealth.Error != null)
        {
            output.Details["error"] = bcHealth.Error;
        }
        
        return output;
    }
}
