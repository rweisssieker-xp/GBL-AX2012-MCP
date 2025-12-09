namespace GBL.AX2012.MCP.Core.Options;

public class AifClientOptions
{
    public const string SectionName = "AifClient";
    
    public string BaseUrl { get; set; } = "http://ax-aos:8101/DynamicsAx/Services";
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    public string Company { get; set; } = "DAT";
    
    /// <summary>
    /// Use NetTcp binding instead of HTTP
    /// </summary>
    public bool UseNetTcp { get; set; } = false;
    
    /// <summary>
    /// NetTcp port (default: 8201)
    /// </summary>
    public int? NetTcpPort { get; set; } = 8201;
    
    /// <summary>
    /// Fallback strategy: "http" (try HTTP first), "nettcp" (try NetTcp first), "auto" (try HTTP, fallback to NetTcp)
    /// </summary>
    public string FallbackStrategy { get; set; } = "auto";
}
