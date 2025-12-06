namespace GBL.AX2012.MCP.Core.Options;

public class WcfClientOptions
{
    public const string SectionName = "WcfClient";
    
    public string BaseUrl { get; set; } = "http://ax-aos:8102/GBL/SalesOrderService.svc";
    public string ServiceAccountUser { get; set; } = "";
    public string ServiceAccountPassword { get; set; } = "";
    public string ServiceAccountDomain { get; set; } = "";
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}
