namespace GBL.AX2012.MCP.BC.Wrapper.Models;

public class HealthCheckResponse
{
    public string Status { get; set; } = "unknown";
    public bool AosConnected { get; set; }
    public long ResponseTimeMs { get; set; }
    public DateTime Timestamp { get; set; }
    public Dictionary<string, string> Details { get; set; } = new();
    public string? Error { get; set; }
}

