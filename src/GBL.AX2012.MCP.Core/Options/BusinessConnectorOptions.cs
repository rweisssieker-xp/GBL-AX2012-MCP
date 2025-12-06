namespace GBL.AX2012.MCP.Core.Options;

public class BusinessConnectorOptions
{
    public const string SectionName = "BusinessConnector";
    
    public string ObjectServer { get; set; } = "ax-aos:2712";
    public string Company { get; set; } = "DAT";
    public string Language { get; set; } = "en-us";
    public string? Configuration { get; set; }
}
