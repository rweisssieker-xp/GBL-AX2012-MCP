using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using GBL.AX2012.MCP.Core.Interfaces;

namespace GBL.AX2012.MCP.Server.Resilience;

public interface ISelfHealingService
{
    Task<SelfHealingStatus> GetStatusAsync(CancellationToken cancellationToken = default);
    void RecordRecovery(string component, string recoveryType);
}

public class SelfHealingStatus
{
    public Dictionary<string, ComponentStatus> CircuitBreakers { get; set; } = new();
    public Dictionary<string, ComponentStatus> ConnectionPools { get; set; } = new();
    public RetryStatistics RetryStats { get; set; } = new();
}

public class ComponentStatus
{
    public string Name { get; set; } = "";
    public string State { get; set; } = "";
    public int AutoRecoveries { get; set; }
    public DateTime? LastRecovery { get; set; }
    public string Status { get; set; } = "";
}

public class RetryStatistics
{
    public int TotalRetries { get; set; }
    public int SuccessfulRetries { get; set; }
    public int FailedRetries { get; set; }
}

public class SelfHealingService : ISelfHealingService, IHostedService
{
    private readonly ILogger<SelfHealingService> _logger;
    private readonly ICircuitBreaker _circuitBreaker;
    private readonly IConnectionPoolMonitor? _connectionPoolMonitor;
    private readonly ConcurrentDictionary<string, ComponentStatus> _componentStatuses = new();
    private readonly RetryStatistics _retryStats = new();
    private Timer? _monitoringTimer;
    
    public SelfHealingService(
        ILogger<SelfHealingService> logger,
        ICircuitBreaker circuitBreaker,
        IConnectionPoolMonitor? connectionPoolMonitor = null)
    {
        _logger = logger;
        _circuitBreaker = circuitBreaker;
        _connectionPoolMonitor = connectionPoolMonitor;
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Self-Healing Service");
        
        // Monitor circuit breaker state every 10 seconds
        _monitoringTimer = new Timer(MonitorComponents, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
        
        return Task.CompletedTask;
    }
    
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _monitoringTimer?.Dispose();
        return Task.CompletedTask;
    }
    
    private void MonitorComponents(object? state)
    {
        try
        {
            // Monitor circuit breaker
            var cbState = _circuitBreaker.State;
            UpdateComponentStatus("ax_connection", "circuit_breaker", cbState.ToString(), "healthy");
            
            // Monitor connection pools
            if (_connectionPoolMonitor != null)
            {
                var poolStatuses = _connectionPoolMonitor.GetAllStatuses();
                foreach (var pool in poolStatuses)
                {
                    UpdateComponentStatus(pool.Key, "connection_pool", pool.Value.Status, pool.Value.Status);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error monitoring components");
        }
    }
    
    private void UpdateComponentStatus(string name, string type, string state, string status)
    {
        var key = $"{type}:{name}";
        var componentStatus = _componentStatuses.GetOrAdd(key, _ => new ComponentStatus { Name = name });
        
        if (componentStatus.State != state && state == "CLOSED" && componentStatus.State == "OPEN")
        {
            // Auto-recovery detected
            componentStatus.AutoRecoveries++;
            componentStatus.LastRecovery = DateTime.UtcNow;
            _logger.LogInformation("Auto-recovery detected for {Component}: {Type} recovered from {OldState} to {NewState}",
                name, type, componentStatus.State, state);
        }
        
        componentStatus.State = state;
        componentStatus.Status = status;
    }
    
    public void RecordRecovery(string component, string recoveryType)
    {
        var key = $"recovery:{component}";
        var status = _componentStatuses.GetOrAdd(key, _ => new ComponentStatus { Name = component });
        status.AutoRecoveries++;
        status.LastRecovery = DateTime.UtcNow;
        status.Status = "recovered";
        
        _logger.LogInformation("Recovery recorded: {Component}, Type: {Type}", component, recoveryType);
    }
    
    public Task<SelfHealingStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var status = new SelfHealingStatus
        {
            CircuitBreakers = _componentStatuses
                .Where(kvp => kvp.Key.StartsWith("circuit_breaker:"))
                .ToDictionary(
                    kvp => kvp.Key.Replace("circuit_breaker:", ""),
                    kvp => kvp.Value),
            ConnectionPools = _componentStatuses
                .Where(kvp => kvp.Key.StartsWith("connection_pool:"))
                .ToDictionary(
                    kvp => kvp.Key.Replace("connection_pool:", ""),
                    kvp => kvp.Value),
            RetryStats = _retryStats
        };
        
        // Add connection pool statuses from monitor if available
        if (_connectionPoolMonitor != null)
        {
            var poolStatuses = _connectionPoolMonitor.GetAllStatuses();
            foreach (var pool in poolStatuses)
            {
                status.ConnectionPools[pool.Key] = new ComponentStatus
                {
                    Name = pool.Value.Name,
                    State = pool.Value.Status,
                    Status = pool.Value.Status,
                    AutoRecoveries = pool.Value.RecoveredConnections,
                    LastRecovery = pool.Value.LastRecovery
                };
            }
        }
        
        return Task.FromResult(status);
    }
}

