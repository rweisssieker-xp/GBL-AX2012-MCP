namespace GBL.AX2012.MCP.Core.Models;

public class ToolContext
{
    public string UserId { get; set; } = "anonymous";
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
    public string[] Roles { get; set; } = [];
    public IDictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    
    public bool HasRole(string role) => Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
    public bool HasAnyRole(params string[] roles) => roles.Any(HasRole);
}
