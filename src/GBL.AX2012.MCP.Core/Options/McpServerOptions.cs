namespace GBL.AX2012.MCP.Core.Options;

public class McpServerOptions
{
    public const string SectionName = "McpServer";
    
    public string Transport { get; set; } = "stdio";
    public string ServerName { get; set; } = "gbl-ax2012-mcp";
    public string ServerVersion { get; set; } = "1.0.0";
}
