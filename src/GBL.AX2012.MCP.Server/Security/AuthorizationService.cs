using Microsoft.Extensions.Logging;
using GBL.AX2012.MCP.Core.Exceptions;
using GBL.AX2012.MCP.Core.Models;

namespace GBL.AX2012.MCP.Server.Security;

public interface IAuthorizationService
{
    bool IsAuthorized(ToolContext context, string[] requiredRoles);
    void EnsureAuthorized(ToolContext context, string[] requiredRoles);
}

public class AuthorizationService : IAuthorizationService
{
    private readonly ILogger<AuthorizationService> _logger;
    
    public AuthorizationService(ILogger<AuthorizationService> logger)
    {
        _logger = logger;
    }
    
    public bool IsAuthorized(ToolContext context, string[] requiredRoles)
    {
        if (requiredRoles == null || requiredRoles.Length == 0)
        {
            return true;
        }
        
        return requiredRoles.Any(role => context.Roles.Contains(role, StringComparer.OrdinalIgnoreCase));
    }
    
    public void EnsureAuthorized(ToolContext context, string[] requiredRoles)
    {
        if (!IsAuthorized(context, requiredRoles))
        {
            _logger.LogWarning("User {UserId} denied access. Required: {Required}, Has: {Has}",
                context.UserId, string.Join(", ", requiredRoles), string.Join(", ", context.Roles));
            
            throw new ForbiddenException(
                $"Access denied. Required roles: {string.Join(" or ", requiredRoles)}");
        }
    }
}

public static class ToolRoleMapping
{
    private static readonly Dictionary<string, string[]> _mapping = new()
    {
        ["ax_health_check"] = ["MCP_Read"],
        ["ax_get_customer"] = ["MCP_Read"],
        ["ax_get_salesorder"] = ["MCP_Read"],
        ["ax_check_inventory"] = ["MCP_Read"],
        ["ax_simulate_price"] = ["MCP_Read"],
        ["ax_create_salesorder"] = ["MCP_Write"],
        ["ax_update_salesorder"] = ["MCP_Write"],
        ["ax_query_audit"] = ["MCP_Admin"],
        ["ax_batch_operations"] = ["MCP_Write"],
        ["ax_subscribe_webhook"] = ["MCP_Admin"],
        ["ax_list_webhooks"] = ["MCP_Admin"],
        ["ax_unsubscribe_webhook"] = ["MCP_Admin"],
        ["ax_get_roi_metrics"] = ["MCP_Admin"]
    };
    
    public static string[] GetRequiredRoles(string toolName)
    {
        return _mapping.TryGetValue(toolName, out var roles) ? roles : ["MCP_Read"];
    }
}
