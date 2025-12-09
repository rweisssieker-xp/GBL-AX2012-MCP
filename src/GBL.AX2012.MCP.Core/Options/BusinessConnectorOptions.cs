namespace GBL.AX2012.MCP.Core.Options;

public class BusinessConnectorOptions
{
    public const string SectionName = "BusinessConnector";
    
    public string ObjectServer { get; set; } = "ax-aos:2712";
    public string Company { get; set; } = "DAT";
    public string Language { get; set; } = "en-us";
    public string? Configuration { get; set; }
    
    /// <summary>
    /// URL of the BC.Wrapper service (if using wrapper instead of direct BC.NET)
    /// Leave empty to use direct BC.NET (requires .NET Framework)
    /// </summary>
    public string? WrapperUrl { get; set; }
    
    /// <summary>
    /// Use wrapper service instead of direct BC.NET
    /// </summary>
    public bool UseWrapper { get; set; } = false;
}
