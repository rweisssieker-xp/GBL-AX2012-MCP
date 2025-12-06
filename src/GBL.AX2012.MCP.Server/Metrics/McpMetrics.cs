using Prometheus;
using GBL.AX2012.MCP.Core.Interfaces;

namespace GBL.AX2012.MCP.Server.Metrics;

public static class McpMetrics
{
    private static readonly Counter ToolCallsTotal = Prometheus.Metrics.CreateCounter(
        "mcp_tool_calls_total",
        "Total number of MCP tool calls",
        new CounterConfiguration
        {
            LabelNames = new[] { "tool", "status" }
        });
    
    private static readonly Histogram ToolCallDuration = Prometheus.Metrics.CreateHistogram(
        "mcp_tool_call_duration_seconds",
        "Duration of MCP tool calls in seconds",
        new HistogramConfiguration
        {
            LabelNames = new[] { "tool" },
            Buckets = new[] { 0.05, 0.1, 0.25, 0.5, 1, 2, 5, 10 }
        });
    
    private static readonly Gauge CircuitBreakerStateGauge = Prometheus.Metrics.CreateGauge(
        "mcp_circuit_breaker_state",
        "Circuit breaker state (0=closed, 1=open, 2=half-open)");
    
    private static readonly Counter RateLimitHitsTotal = Prometheus.Metrics.CreateCounter(
        "mcp_rate_limit_hits_total",
        "Total number of rate limit hits",
        new CounterConfiguration
        {
            LabelNames = new[] { "user" }
        });
    
    private static readonly Gauge ActiveConnectionsGauge = Prometheus.Metrics.CreateGauge(
        "mcp_active_connections",
        "Number of active MCP connections");
    
    private static readonly Counter AxCallsTotal = Prometheus.Metrics.CreateCounter(
        "mcp_ax_calls_total",
        "Total number of AX service calls",
        new CounterConfiguration
        {
            LabelNames = new[] { "service", "operation", "status" }
        });
    
    private static readonly Histogram AxCallDuration = Prometheus.Metrics.CreateHistogram(
        "mcp_ax_call_duration_seconds",
        "Duration of AX service calls in seconds",
        new HistogramConfiguration
        {
            LabelNames = new[] { "service", "operation" },
            Buckets = new[] { 0.1, 0.25, 0.5, 1, 2, 5, 10, 30 }
        });
    
    private static readonly Counter ErrorsTotal = Prometheus.Metrics.CreateCounter(
        "mcp_errors_total",
        "Total number of errors",
        new CounterConfiguration
        {
            LabelNames = new[] { "error_code", "tool" }
        });
    
    private static readonly Gauge UptimeSeconds = Prometheus.Metrics.CreateGauge(
        "mcp_uptime_seconds",
        "Server uptime in seconds");
    
    private static readonly DateTime StartTime = DateTime.UtcNow;
    
    public static void RecordToolCall(string tool, bool success, double durationSeconds)
    {
        ToolCallsTotal.WithLabels(tool, success ? "success" : "error").Inc();
        ToolCallDuration.WithLabels(tool).Observe(durationSeconds);
    }
    
    public static void RecordError(string errorCode, string tool)
    {
        ErrorsTotal.WithLabels(errorCode, tool).Inc();
    }
    
    public static void RecordRateLimitHit(string user)
    {
        RateLimitHitsTotal.WithLabels(user).Inc();
    }
    
    public static void SetCircuitBreakerState(CircuitState state)
    {
        CircuitBreakerStateGauge.Set(state switch
        {
            CircuitState.Closed => 0,
            CircuitState.Open => 1,
            CircuitState.HalfOpen => 2,
            _ => -1
        });
    }
    
    public static void IncrementActiveConnections() => ActiveConnectionsGauge.Inc();
    public static void DecrementActiveConnections() => ActiveConnectionsGauge.Dec();
    
    public static void RecordAxCall(string service, string operation, bool success, double durationSeconds)
    {
        AxCallsTotal.WithLabels(service, operation, success ? "success" : "error").Inc();
        AxCallDuration.WithLabels(service, operation).Observe(durationSeconds);
    }
    
    public static void UpdateUptime()
    {
        UptimeSeconds.Set((DateTime.UtcNow - StartTime).TotalSeconds);
    }
}
