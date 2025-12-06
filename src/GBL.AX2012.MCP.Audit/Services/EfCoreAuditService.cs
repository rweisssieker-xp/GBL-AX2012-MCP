using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using GBL.AX2012.MCP.Core.Interfaces;
using GBL.AX2012.MCP.Core.Models;
using GBL.AX2012.MCP.Audit.Data;

namespace GBL.AX2012.MCP.Audit.Services;

public class EfCoreAuditService : IAuditService
{
    private readonly IDbContextFactory<AuditDbContext> _contextFactory;
    private readonly ILogger<EfCoreAuditService> _logger;
    
    public EfCoreAuditService(
        IDbContextFactory<AuditDbContext> contextFactory,
        ILogger<EfCoreAuditService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }
    
    public async Task LogAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            
            var entity = AuditEntryEntity.FromModel(entry);
            context.AuditEntries.Add(entity);
            
            await context.SaveChangesAsync(cancellationToken);
            
            _logger.LogDebug("Audit entry saved: {ToolName} by {UserId}", entry.ToolName, entry.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save audit entry for {ToolName}", entry.ToolName);
            // Don't throw - audit failures shouldn't break the main flow
        }
    }
    
    public async Task<IEnumerable<AuditEntry>> QueryAsync(AuditQuery query, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        
        var queryable = context.AuditEntries.AsQueryable();
        
        // Apply filters
        if (!string.IsNullOrEmpty(query.UserId))
            queryable = queryable.Where(e => e.UserId == query.UserId);
        
        if (!string.IsNullOrEmpty(query.ToolName))
            queryable = queryable.Where(e => e.ToolName == query.ToolName);
        
        if (!string.IsNullOrEmpty(query.CorrelationId))
            queryable = queryable.Where(e => e.CorrelationId == query.CorrelationId);
        
        if (query.Success.HasValue)
            queryable = queryable.Where(e => e.Success == query.Success.Value);
        
        // Date filters (support both old and new property names)
        var fromDate = query.FromDate ?? query.DateFrom;
        var toDate = query.ToDate ?? query.DateTo;
        
        if (fromDate.HasValue)
            queryable = queryable.Where(e => e.Timestamp >= fromDate.Value);
        
        if (toDate.HasValue)
            queryable = queryable.Where(e => e.Timestamp <= toDate.Value);
        
        // Order and paginate
        var results = await queryable
            .OrderByDescending(e => e.Timestamp)
            .Skip(query.Skip)
            .Take(Math.Min(query.Take, query.MaxResults))
            .ToListAsync(cancellationToken);
        
        return results.Select(e => e.ToModel());
    }
    
    public async Task<AuditStats> GetStatsAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        
        var entries = await context.AuditEntries
            .Where(e => e.Timestamp >= from && e.Timestamp <= to)
            .ToListAsync(cancellationToken);
        
        return new AuditStats
        {
            TotalCalls = entries.Count,
            SuccessfulCalls = entries.Count(e => e.Success),
            FailedCalls = entries.Count(e => !e.Success),
            AverageDurationMs = entries.Any() ? entries.Average(e => e.DurationMs) : 0,
            CallsByTool = entries.GroupBy(e => e.ToolName).ToDictionary(g => g.Key, g => g.Count()),
            CallsByUser = entries.GroupBy(e => e.UserId).ToDictionary(g => g.Key, g => g.Count()),
            ErrorsByType = entries.Where(e => !string.IsNullOrEmpty(e.Error))
                .GroupBy(e => e.Error!.Split(':').First())
                .ToDictionary(g => g.Key, g => g.Count())
        };
    }
}

public class AuditStats
{
    public int TotalCalls { get; set; }
    public int SuccessfulCalls { get; set; }
    public int FailedCalls { get; set; }
    public double AverageDurationMs { get; set; }
    public Dictionary<string, int> CallsByTool { get; set; } = new();
    public Dictionary<string, int> CallsByUser { get; set; } = new();
    public Dictionary<string, int> ErrorsByType { get; set; } = new();
}
