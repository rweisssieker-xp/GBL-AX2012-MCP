namespace GBL.AX2012.MCP.Core.Options;

public class RateLimiterOptions
{
    public const string SectionName = "RateLimiter";
    
    public int RequestsPerMinute { get; set; } = 100;
    public bool Enabled { get; set; } = true;
}
