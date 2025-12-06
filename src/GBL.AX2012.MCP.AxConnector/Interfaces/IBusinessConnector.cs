namespace GBL.AX2012.MCP.AxConnector.Interfaces;

public interface IBusinessConnector : IDisposable
{
    Task<AxHealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default);
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
    bool IsConnected { get; }
}

public class AxHealthCheckResult
{
    public string Status { get; set; } = "unknown";
    public bool AosConnected { get; set; }
    public long ResponseTimeMs { get; set; }
    public DateTime Timestamp { get; set; }
    public Dictionary<string, string> Details { get; set; } = new();
    public string? Error { get; set; }
}
