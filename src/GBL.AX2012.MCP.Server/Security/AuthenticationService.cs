using System.Security.Principal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using GBL.AX2012.MCP.Core.Options;

namespace GBL.AX2012.MCP.Server.Security;

public interface IAuthenticationService
{
    Task<AuthenticationResult> AuthenticateAsync(CancellationToken cancellationToken = default);
    string? GetCurrentUserId();
    string[] GetCurrentUserRoles();
}

public class AuthenticationResult
{
    public bool IsAuthenticated { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string[] Roles { get; set; } = [];
    public string? ErrorMessage { get; set; }
}

public class WindowsAuthenticationService : IAuthenticationService
{
    private readonly ILogger<WindowsAuthenticationService> _logger;
    private readonly SecurityOptions _options;
    private AuthenticationResult? _cachedResult;
    
    public WindowsAuthenticationService(
        ILogger<WindowsAuthenticationService> logger,
        IOptions<SecurityOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }
    
    public Task<AuthenticationResult> AuthenticateAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedResult != null)
        {
            return Task.FromResult(_cachedResult);
        }
        
        try
        {
            var identity = WindowsIdentity.GetCurrent();
            
            if (identity == null || !identity.IsAuthenticated)
            {
                _logger.LogWarning("No authenticated Windows identity found");
                return Task.FromResult(new AuthenticationResult
                {
                    IsAuthenticated = false,
                    ErrorMessage = "Not authenticated"
                });
            }
            
            var roles = GetRolesFromGroups(identity);
            
            _cachedResult = new AuthenticationResult
            {
                IsAuthenticated = true,
                UserId = identity.Name,
                UserName = identity.Name.Split('\\').LastOrDefault() ?? identity.Name,
                Roles = roles
            };
            
            _logger.LogDebug("Authenticated user {UserId} with roles {Roles}", 
                _cachedResult.UserId, string.Join(", ", roles));
            
            return Task.FromResult(_cachedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Authentication failed");
            return Task.FromResult(new AuthenticationResult
            {
                IsAuthenticated = false,
                ErrorMessage = ex.Message
            });
        }
    }
    
    public string? GetCurrentUserId()
    {
        return _cachedResult?.UserId ?? WindowsIdentity.GetCurrent()?.Name;
    }
    
    public string[] GetCurrentUserRoles()
    {
        return _cachedResult?.Roles ?? [];
    }
    
    private string[] GetRolesFromGroups(WindowsIdentity identity)
    {
        var roles = new List<string>();
        
        if (identity.Groups == null)
        {
            // Default to read access if no groups
            return ["MCP_Read"];
        }
        
        foreach (var group in identity.Groups)
        {
            try
            {
                var groupName = group.Translate(typeof(NTAccount))?.Value;
                if (groupName == null) continue;
                
                if (groupName.Contains("MCP-Users-Read", StringComparison.OrdinalIgnoreCase) ||
                    groupName.Contains("MCP_Read", StringComparison.OrdinalIgnoreCase))
                {
                    roles.Add("MCP_Read");
                }
                else if (groupName.Contains("MCP-Users-Write", StringComparison.OrdinalIgnoreCase) ||
                         groupName.Contains("MCP_Write", StringComparison.OrdinalIgnoreCase))
                {
                    roles.Add("MCP_Write");
                    roles.Add("MCP_Read");
                }
                else if (groupName.Contains("MCP-Admins", StringComparison.OrdinalIgnoreCase) ||
                         groupName.Contains("MCP_Admin", StringComparison.OrdinalIgnoreCase))
                {
                    roles.Add("MCP_Admin");
                    roles.Add("MCP_Write");
                    roles.Add("MCP_Read");
                }
            }
            catch (IdentityNotMappedException)
            {
                // Skip unmapped groups
            }
        }
        
        // Default to read access if no MCP groups found
        if (!roles.Any())
        {
            roles.Add("MCP_Read");
        }
        
        return roles.Distinct().ToArray();
    }
}
