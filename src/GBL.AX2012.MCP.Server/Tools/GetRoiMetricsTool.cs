using FluentValidation;
using Microsoft.Extensions.Logging;
using GBL.AX2012.MCP.Core.Interfaces;
using GBL.AX2012.MCP.Core.Models;

namespace GBL.AX2012.MCP.Server.Tools;

public class GetRoiMetricsInput
{
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string? GroupBy { get; set; } // "tool", "user", "department"
}

public class RoiMetricsByTool
{
    public string Tool { get; set; } = "";
    public int Operations { get; set; }
    public double AvgTimeSavedSec { get; set; }
    public double TotalTimeSavedHours { get; set; }
    public decimal CostSavedEur { get; set; }
}

public class GetRoiMetricsOutput
{
    public int TotalOperations { get; set; }
    public double TotalTimeSavedHours { get; set; }
    public decimal TotalCostSavedEur { get; set; }
    public List<RoiMetricsByTool> ByTool { get; set; } = new();
    public Dictionary<string, object>? ByUser { get; set; }
    public Dictionary<string, object>? ByDepartment { get; set; }
}

public class GetRoiMetricsInputValidator : AbstractValidator<GetRoiMetricsInput>
{
    public GetRoiMetricsInputValidator()
    {
        RuleFor(x => x.GroupBy)
            .Must(g => g == null || new[] { "tool", "user", "department" }.Contains(g))
            .WithMessage("group_by must be one of: tool, user, department");
    }
}

public class GetRoiMetricsTool : ToolBase<GetRoiMetricsInput, GetRoiMetricsOutput>
{
    private readonly IAuditService _auditService;
    private readonly decimal _hourlyRate = 50m; // Configurable
    
    // Baseline times in seconds (how long manual operation takes)
    private readonly Dictionary<string, int> _baselineTimes = new()
    {
        { "ax_create_salesorder", 300 }, // 5 minutes
        { "ax_get_customer", 30 },
        { "ax_get_salesorder", 45 },
        { "ax_check_inventory", 60 },
        { "ax_simulate_price", 120 },
        { "ax_update_salesorder", 180 },
        { "ax_create_invoice", 240 },
        { "ax_post_payment", 180 }
    };
    
    public override string Name => "ax_get_roi_metrics";
    public override string Description => "Get ROI metrics for MCP operations";
    
    public GetRoiMetricsTool(
        ILogger<GetRoiMetricsTool> logger,
        IAuditService auditService,
        GetRoiMetricsInputValidator validator)
        : base(logger, auditService, validator)
    {
        _auditService = auditService;
    }
    
    protected override async Task<GetRoiMetricsOutput> ExecuteCoreAsync(
        GetRoiMetricsInput input,
        ToolContext context,
        CancellationToken cancellationToken)
    {
        var dateFrom = input.DateFrom ?? DateTime.UtcNow.AddDays(-30);
        var dateTo = input.DateTo ?? DateTime.UtcNow;
        
        // Query audit log for successful operations in date range
        var query = new AuditQuery
        {
            DateFrom = dateFrom,
            DateTo = dateTo,
            Success = true,
            MaxResults = 10000
        };
        
        var auditEntries = await _auditService.QueryAsync(query, cancellationToken);
        var entries = auditEntries.ToList();
        
        var metrics = new GetRoiMetricsOutput
        {
            TotalOperations = entries.Count,
            TotalTimeSavedHours = 0,
            TotalCostSavedEur = 0,
            ByTool = new List<RoiMetricsByTool>()
        };
        
        // Group by tool
        var toolGroups = entries
            .Where(e => _baselineTimes.ContainsKey(e.ToolName))
            .GroupBy(e => e.ToolName)
            .ToList();
        
        foreach (var group in toolGroups)
        {
            var toolName = group.Key;
            var baselineSec = _baselineTimes[toolName];
            var operations = group.ToList();
            
            // Calculate time saved: baseline - actual duration
            // Assume 90% time saved (MCP is much faster than manual)
            var avgTimeSavedSec = baselineSec * 0.9;
            var totalTimeSavedHours = (operations.Count * avgTimeSavedSec) / 3600.0;
            var costSavedEur = (decimal)totalTimeSavedHours * _hourlyRate;
            
            var toolMetrics = new RoiMetricsByTool
            {
                Tool = toolName,
                Operations = operations.Count,
                AvgTimeSavedSec = avgTimeSavedSec,
                TotalTimeSavedHours = totalTimeSavedHours,
                CostSavedEur = costSavedEur
            };
            
            metrics.ByTool.Add(toolMetrics);
            metrics.TotalTimeSavedHours += totalTimeSavedHours;
            metrics.TotalCostSavedEur += costSavedEur;
        }
        
        // Group by user if requested
        if (input.GroupBy == "user")
        {
            var userGroups = entries
                .Where(e => _baselineTimes.ContainsKey(e.ToolName))
                .GroupBy(e => e.UserId)
                .ToDictionary(g => g.Key, g =>
                {
                    var userOps = g.ToList();
                    var totalSaved = userOps.Sum(op => _baselineTimes.GetValueOrDefault(op.ToolName, 0) * 0.9 / 3600.0);
                    return new
                    {
                        Operations = userOps.Count,
                        TimeSavedHours = totalSaved,
                        CostSavedEur = (decimal)totalSaved * _hourlyRate
                    };
                });
            
            metrics.ByUser = userGroups.ToDictionary(
                kvp => kvp.Key,
                kvp => (object)new { kvp.Value.Operations, kvp.Value.TimeSavedHours, kvp.Value.CostSavedEur });
        }
        
        _logger.LogInformation("ROI metrics calculated: {TotalOps} operations, {Hours:F2} hours saved, {Cost:F2} EUR saved",
            metrics.TotalOperations, metrics.TotalTimeSavedHours, metrics.TotalCostSavedEur);
        
        return metrics;
    }
}

