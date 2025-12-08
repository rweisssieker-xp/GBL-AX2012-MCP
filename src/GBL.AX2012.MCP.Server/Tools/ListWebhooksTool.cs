using FluentValidation;
using Microsoft.Extensions.Logging;
using GBL.AX2012.MCP.Core.Interfaces;
using GBL.AX2012.MCP.Core.Models;
using GBL.AX2012.MCP.Server.Webhooks;

namespace GBL.AX2012.MCP.Server.Tools;

public class ListWebhooksInput
{
    public string? EventType { get; set; }
}

public class WebhookSubscriptionOutput
{
    public Guid SubscriptionId { get; set; }
    public string EventType { get; set; } = "";
    public string WebhookUrl { get; set; } = "";
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastTriggeredAt { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
}

public class ListWebhooksOutput
{
    public List<WebhookSubscriptionOutput> Subscriptions { get; set; } = new();
}

public class ListWebhooksTool : ToolBase<ListWebhooksInput, ListWebhooksOutput>
{
    private readonly IWebhookService _webhookService;
    
    public override string Name => "ax_list_webhooks";
    public override string Description => "List all webhook subscriptions";
    
    public ListWebhooksTool(
        ILogger<ListWebhooksTool> logger,
        IAuditService audit,
        IWebhookService webhookService)
        : base(logger, audit, null)
    {
        _webhookService = webhookService;
    }
    
    protected override async Task<ListWebhooksOutput> ExecuteCoreAsync(
        ListWebhooksInput input,
        ToolContext context,
        CancellationToken cancellationToken)
    {
        var subscriptions = await _webhookService.ListSubscriptionsAsync(input.EventType, cancellationToken);
        
        return new ListWebhooksOutput
        {
            Subscriptions = subscriptions.Select(s => new WebhookSubscriptionOutput
            {
                SubscriptionId = s.Id,
                EventType = s.EventType,
                WebhookUrl = s.WebhookUrl,
                IsActive = s.IsActive,
                CreatedAt = s.CreatedAt,
                LastTriggeredAt = s.LastTriggeredAt,
                SuccessCount = s.SuccessCount,
                FailureCount = s.FailureCount
            }).ToList()
        };
    }
}

