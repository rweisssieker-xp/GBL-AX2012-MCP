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
        // Note: This is a simplified implementation. In production, you'd query the audit database
        var metrics = new GetRoiMetricsOutput
        {
            TotalOperations = 0,
            TotalTimeSavedHours = 0,
            TotalCostSavedEur = 0,
            ByTool = new List<RoiMetricsByTool>()
        };
        
        // Group by tool
        var toolGroups = new Dictionary<string, RoiMetricsByTool>();
        
        // Simulate metrics calculation (in production, query audit database)
        // For now, return sample data structure
        foreach (var baseline in _baselineTimes)
        {
            var toolMetrics = new RoiMetricsByTool
            {
                Tool = baseline.Key,
                Operations = 100, // Sample
                AvgTimeSavedSec = baseline.Value * 0.9, // Assume 90% time saved
                TotalTimeSavedHours = 0,
                CostSavedEur = 0
            };
            
            toolMetrics.TotalTimeSavedHours = (toolMetrics.Operations * toolMetrics.AvgTimeSavedSec) / 3600.0;
            toolMetrics.CostSavedEur = (decimal)toolMetrics.TotalTimeSavedHours * _hourlyRate;
            
            metrics.ByTool.Add(toolMetrics);
            metrics.TotalOperations += toolMetrics.Operations;
            metrics.TotalTimeSavedHours += toolMetrics.TotalTimeSavedHours;
            metrics.TotalCostSavedEur += toolMetrics.CostSavedEur;
        }
        
        _logger.LogInformation("ROI metrics calculated: {TotalOps} operations, {Hours} hours saved, {Cost} EUR saved",
            metrics.TotalOperations, metrics.TotalTimeSavedHours, metrics.TotalCostSavedEur);
        
        return metrics;
    }
}

