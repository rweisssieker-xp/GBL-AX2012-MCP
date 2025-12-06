namespace GBL.AX2012.MCP.Core.Models;

public class AuditEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string UserId { get; set; } = "";
    public string ToolName { get; set; } = "";
    public string? CorrelationId { get; set; }
    public string? Input { get; set; }
    public string? Output { get; set; }
    public bool Success { get; set; }
    public long DurationMs { get; set; }
    public string? Error { get; set; }
}

public class AuditQuery
{
    public string? UserId { get; set; }
    public string? ToolName { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public bool? Success { get; set; }
    public string? CorrelationId { get; set; }
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 50;
    public int MaxResults { get; set; } = 100;
}
