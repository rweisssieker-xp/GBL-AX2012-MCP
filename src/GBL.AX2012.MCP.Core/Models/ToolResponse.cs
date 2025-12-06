namespace GBL.AX2012.MCP.Core.Models;

public class ToolResponse
{
    public bool Success { get; set; }
    public object? Data { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public long DurationMs { get; set; }
    
    public static ToolResponse Ok(object data) => new() { Success = true, Data = data };
    public static ToolResponse Error(string code, string message) => new() { Success = false, ErrorCode = code, ErrorMessage = message };
}
