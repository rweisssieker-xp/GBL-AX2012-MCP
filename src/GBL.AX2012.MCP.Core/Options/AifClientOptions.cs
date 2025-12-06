namespace GBL.AX2012.MCP.Core.Options;

public class AifClientOptions
{
    public const string SectionName = "AifClient";
    
    public string BaseUrl { get; set; } = "http://ax-aos:8101/DynamicsAx/Services";
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    public string Company { get; set; } = "DAT";
}
