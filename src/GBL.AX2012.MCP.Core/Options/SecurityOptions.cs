namespace GBL.AX2012.MCP.Core.Options;

public class SecurityOptions
{
    public const string SectionName = "Security";
    
    public bool RequireAuthentication { get; set; } = true;
    public string[] AllowedRoles { get; set; } = ["MCP_Read", "MCP_Write", "MCP_Admin"];
    public decimal ApprovalThreshold { get; set; } = 50000m;
}
