namespace GBL.AX2012.MCP.Core.Options;

public class AuditOptions
{
    public const string SectionName = "Audit";
    
    public string DatabaseConnectionString { get; set; } = "";
    public string FileLogPath { get; set; } = "logs/audit";
    public int RetentionDays { get; set; } = 90;
}
