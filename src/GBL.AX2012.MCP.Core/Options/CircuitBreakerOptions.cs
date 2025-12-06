namespace GBL.AX2012.MCP.Core.Options;

public class CircuitBreakerOptions
{
    public const string SectionName = "CircuitBreaker";
    
    public int FailureThreshold { get; set; } = 3;
    public TimeSpan OpenDuration { get; set; } = TimeSpan.FromSeconds(60);
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}
