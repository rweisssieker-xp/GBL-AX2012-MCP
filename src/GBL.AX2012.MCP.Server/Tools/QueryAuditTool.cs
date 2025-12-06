using FluentValidation;
using Microsoft.Extensions.Logging;
using GBL.AX2012.MCP.Core.Interfaces;
using GBL.AX2012.MCP.Core.Models;

namespace GBL.AX2012.MCP.Server.Tools;

public class QueryAuditInput
{
    public string? UserId { get; set; }
    public string? ToolName { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public bool? SuccessOnly { get; set; }
    public int MaxResults { get; set; } = 100;
}

public class QueryAuditOutput
{
    public List<AuditEntryOutput> Entries { get; set; } = new();
    public int TotalCount { get; set; }
    public QueryAuditSummary Summary { get; set; } = new();
}

public class AuditEntryOutput
{
    public string Id { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public string UserId { get; set; } = "";
    public string ToolName { get; set; } = "";
    public string CorrelationId { get; set; } = "";
    public bool Success { get; set; }
    public int DurationMs { get; set; }
    public string? Error { get; set; }
}

public class QueryAuditSummary
{
    public int TotalCalls { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public double SuccessRate { get; set; }
    public double AvgDurationMs { get; set; }
    public Dictionary<string, int> CallsByTool { get; set; } = new();
    public Dictionary<string, int> CallsByUser { get; set; } = new();
}

public class QueryAuditInputValidator : AbstractValidator<QueryAuditInput>
{
    public QueryAuditInputValidator()
    {
        RuleFor(x => x.MaxResults).InclusiveBetween(1, 1000).WithMessage("max_results must be between 1 and 1000");
    }
}

public class QueryAuditTool : ToolBase<QueryAuditInput, QueryAuditOutput>
{
    private readonly IAuditService _auditService;
    
    public override string Name => "ax_query_audit";
    public override string Description => "Query audit log entries (Admin only)";
    
    public QueryAuditTool(
        ILogger<QueryAuditTool> logger,
        IAuditService audit,
        QueryAuditInputValidator validator)
        : base(logger, audit, validator)
    {
        _auditService = audit;
    }
    
    protected override async Task<QueryAuditOutput> ExecuteCoreAsync(
        QueryAuditInput input, 
        ToolContext context, 
        CancellationToken cancellationToken)
    {
        // Check admin role
        if (!context.HasRole("MCP_Admin"))
        {
            throw new Core.Exceptions.ForbiddenException("Audit query requires MCP_Admin role");
        }
        
        var query = new AuditQuery
        {
            UserId = input.UserId,
            ToolName = input.ToolName,
            FromDate = input.FromDate ?? DateTime.Today.AddDays(-7),
            ToDate = input.ToDate ?? DateTime.Now,
            MaxResults = input.MaxResults
        };
        
        var entries = await _auditService.QueryAsync(query, cancellationToken);
        var entryList = entries.ToList();
        
        if (input.SuccessOnly.HasValue)
        {
            entryList = entryList.Where(e => e.Success == input.SuccessOnly.Value).ToList();
        }
        
        var output = new QueryAuditOutput
        {
            Entries = entryList.Select(e => new AuditEntryOutput
            {
                Id = e.Id.ToString(),
                Timestamp = e.Timestamp,
                UserId = e.UserId,
                ToolName = e.ToolName,
                CorrelationId = e.CorrelationId ?? "",
                Success = e.Success,
                DurationMs = (int)e.DurationMs,
                Error = e.Error
            }).ToList(),
            TotalCount = entryList.Count,
            Summary = new QueryAuditSummary
            {
                TotalCalls = entryList.Count,
                SuccessCount = entryList.Count(e => e.Success),
                ErrorCount = entryList.Count(e => !e.Success),
                SuccessRate = entryList.Count > 0 ? (double)entryList.Count(e => e.Success) / entryList.Count * 100 : 0,
                AvgDurationMs = entryList.Count > 0 ? entryList.Average(e => e.DurationMs) : 0,
                CallsByTool = entryList.GroupBy(e => e.ToolName).ToDictionary(g => g.Key, g => g.Count()),
                CallsByUser = entryList.GroupBy(e => e.UserId).ToDictionary(g => g.Key, g => g.Count())
            }
        };
        
        return output;
    }
}
