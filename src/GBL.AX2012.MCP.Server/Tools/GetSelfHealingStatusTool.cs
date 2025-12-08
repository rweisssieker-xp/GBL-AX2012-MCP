using Microsoft.Extensions.Logging;
using GBL.AX2012.MCP.Core.Interfaces;
using GBL.AX2012.MCP.Core.Models;
using GBL.AX2012.MCP.Server.Resilience;

namespace GBL.AX2012.MCP.Server.Tools;

public class GetSelfHealingStatusOutput
{
    public Dictionary<string, ComponentStatusOutput> CircuitBreakers { get; set; } = new();
    public Dictionary<string, ComponentStatusOutput> ConnectionPools { get; set; } = new();
    public RetryStatisticsOutput RetryStats { get; set; } = new();
}

public class ComponentStatusOutput
{
    public string Name { get; set; } = "";
    public string State { get; set; } = "";
    public int AutoRecoveries { get; set; }
    public DateTime? LastRecovery { get; set; }
}

public class RetryStatisticsOutput
{
    public int TotalRetries { get; set; }
    public int SuccessfulRetries { get; set; }
    public int FailedRetries { get; set; }
}

public class GetSelfHealingStatusTool : ToolBase<object, GetSelfHealingStatusOutput>
{
    private readonly ISelfHealingService _selfHealingService;
    
    public override string Name => "ax_get_self_healing_status";
    public override string Description => "Get self-healing operations status";
    
    public GetSelfHealingStatusTool(
        ILogger<GetSelfHealingStatusTool> logger,
        IAuditService audit,
        ISelfHealingService selfHealingService)
        : base(logger, audit, null)
    {
        _selfHealingService = selfHealingService;
    }
    
    protected override async Task<GetSelfHealingStatusOutput> ExecuteCoreAsync(
        object input,
        ToolContext context,
        CancellationToken cancellationToken)
    {
        var status = await _selfHealingService.GetStatusAsync(cancellationToken);
        
        return new GetSelfHealingStatusOutput
        {
            CircuitBreakers = status.CircuitBreakers.ToDictionary(
                kvp => kvp.Key,
                kvp => new ComponentStatusOutput
                {
                    Name = kvp.Value.Name,
                    State = kvp.Value.State,
                    AutoRecoveries = kvp.Value.AutoRecoveries,
                    LastRecovery = kvp.Value.LastRecovery
                }),
            ConnectionPools = status.ConnectionPools.ToDictionary(
                kvp => kvp.Key,
                kvp => new ComponentStatusOutput
                {
                    Name = kvp.Value.Name,
                    State = kvp.Value.State,
                    AutoRecoveries = kvp.Value.AutoRecoveries,
                    LastRecovery = kvp.Value.LastRecovery
                }),
            RetryStats = new RetryStatisticsOutput
            {
                TotalRetries = status.RetryStats.TotalRetries,
                SuccessfulRetries = status.RetryStats.SuccessfulRetries,
                FailedRetries = status.RetryStats.FailedRetries
            }
        };
    }
}

