namespace GBL.AX2012.MCP.Core.Models;

public class WebhookSubscription
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = "";
    public string WebhookUrl { get; set; } = "";
    public string? Secret { get; set; }
    public Dictionary<string, object>? Filters { get; set; }
    public WebhookRetryPolicy RetryPolicy { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastTriggeredAt { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
}

public class WebhookRetryPolicy
{
    public int MaxRetries { get; set; } = 3;
    public int BackoffMs { get; set; } = 1000;
    public bool ExponentialBackoff { get; set; } = true;
}

