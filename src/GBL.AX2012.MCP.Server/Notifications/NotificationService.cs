using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using GBL.AX2012.MCP.Core.Options;

namespace GBL.AX2012.MCP.Server.Notifications;

public enum NotificationSeverity
{
    Info,
    Warning,
    Error,
    Critical
}

public interface INotificationService
{
    Task SendAlertAsync(string title, string message, NotificationSeverity severity);
    Task SendSlackAsync(string channel, string message);
    Task SendTeamsAsync(string message);
}

public class NotificationOptions
{
    public bool Enabled { get; set; } = true;
    public string? SlackWebhookUrl { get; set; }
    public string? TeamsWebhookUrl { get; set; }
    public string? DefaultSlackChannel { get; set; } = "#mcp-alerts";
}

public class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;
    private readonly NotificationOptions _options;
    private readonly HttpClient _httpClient;
    
    public NotificationService(
        ILogger<NotificationService> logger,
        IOptions<NotificationOptions> options,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _options = options.Value;
        _httpClient = httpClientFactory.CreateClient("Notifications");
    }
    
    public async Task SendAlertAsync(string title, string message, NotificationSeverity severity)
    {
        if (!_options.Enabled)
        {
            _logger.LogDebug("Notifications disabled, skipping alert: {Title}", title);
            return;
        }
        
        _logger.LogInformation("Sending alert: {Title} ({Severity})", title, severity);
        
        var tasks = new List<Task>();
        
        if (!string.IsNullOrEmpty(_options.SlackWebhookUrl))
        {
            tasks.Add(SendSlackAlertAsync(title, message, severity));
        }
        
        if (!string.IsNullOrEmpty(_options.TeamsWebhookUrl))
        {
            tasks.Add(SendTeamsAlertAsync(title, message, severity));
        }
        
        if (tasks.Any())
        {
            await Task.WhenAll(tasks);
        }
        else
        {
            _logger.LogWarning("No notification webhooks configured");
        }
    }
    
    public async Task SendSlackAsync(string channel, string message)
    {
        if (string.IsNullOrEmpty(_options.SlackWebhookUrl))
        {
            _logger.LogWarning("Slack webhook URL not configured");
            return;
        }
        
        try
        {
            var payload = new
            {
                channel,
                text = message,
                username = "MCP Server",
                icon_emoji = ":robot_face:"
            };
            
            var response = await _httpClient.PostAsJsonAsync(_options.SlackWebhookUrl, payload);
            response.EnsureSuccessStatusCode();
            
            _logger.LogDebug("Slack message sent to {Channel}", channel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Slack message");
        }
    }
    
    public async Task SendTeamsAsync(string message)
    {
        if (string.IsNullOrEmpty(_options.TeamsWebhookUrl))
        {
            _logger.LogWarning("Teams webhook URL not configured");
            return;
        }
        
        try
        {
            var payload = new
            {
                text = message
            };
            
            var response = await _httpClient.PostAsJsonAsync(_options.TeamsWebhookUrl, payload);
            response.EnsureSuccessStatusCode();
            
            _logger.LogDebug("Teams message sent");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Teams message");
        }
    }
    
    private async Task SendSlackAlertAsync(string title, string message, NotificationSeverity severity)
    {
        var color = severity switch
        {
            NotificationSeverity.Critical => "#FF0000",
            NotificationSeverity.Error => "#FF6600",
            NotificationSeverity.Warning => "#FFCC00",
            _ => "#36A64F"
        };
        
        var emoji = severity switch
        {
            NotificationSeverity.Critical => ":rotating_light:",
            NotificationSeverity.Error => ":x:",
            NotificationSeverity.Warning => ":warning:",
            _ => ":information_source:"
        };
        
        try
        {
            var payload = new
            {
                channel = _options.DefaultSlackChannel,
                username = "MCP Server",
                icon_emoji = emoji,
                attachments = new[]
                {
                    new
                    {
                        color,
                        title = $"{emoji} {title}",
                        text = message,
                        footer = "GBL-AX2012-MCP",
                        ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                    }
                }
            };
            
            var response = await _httpClient.PostAsJsonAsync(_options.SlackWebhookUrl!, payload);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Slack alert");
        }
    }
    
    private async Task SendTeamsAlertAsync(string title, string message, NotificationSeverity severity)
    {
        var themeColor = severity switch
        {
            NotificationSeverity.Critical => "FF0000",
            NotificationSeverity.Error => "FF6600",
            NotificationSeverity.Warning => "FFCC00",
            _ => "36A64F"
        };
        
        try
        {
            var payload = new
            {
                @type = "MessageCard",
                themeColor,
                title,
                text = message,
                sections = new[]
                {
                    new
                    {
                        facts = new[]
                        {
                            new { name = "Severity", value = severity.ToString() },
                            new { name = "Time", value = DateTime.UtcNow.ToString("u") },
                            new { name = "Server", value = Environment.MachineName }
                        }
                    }
                }
            };
            
            var response = await _httpClient.PostAsJsonAsync(_options.TeamsWebhookUrl!, payload);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Teams alert");
        }
    }
}

// Fallback implementation when no webhooks configured
public class NullNotificationService : INotificationService
{
    private readonly ILogger<NullNotificationService> _logger;
    
    public NullNotificationService(ILogger<NullNotificationService> logger)
    {
        _logger = logger;
    }
    
    public Task SendAlertAsync(string title, string message, NotificationSeverity severity)
    {
        _logger.LogInformation("[NOTIFICATION] {Severity}: {Title} - {Message}", severity, title, message);
        return Task.CompletedTask;
    }
    
    public Task SendSlackAsync(string channel, string message)
    {
        _logger.LogInformation("[SLACK] #{Channel}: {Message}", channel, message);
        return Task.CompletedTask;
    }
    
    public Task SendTeamsAsync(string message)
    {
        _logger.LogInformation("[TEAMS]: {Message}", message);
        return Task.CompletedTask;
    }
}
