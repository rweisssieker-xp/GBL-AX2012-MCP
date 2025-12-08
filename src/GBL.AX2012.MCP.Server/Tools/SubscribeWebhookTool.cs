using FluentValidation;
using Microsoft.Extensions.Logging;
using GBL.AX2012.MCP.Core.Interfaces;
using GBL.AX2012.MCP.Core.Models;
using GBL.AX2012.MCP.Server.Webhooks;

namespace GBL.AX2012.MCP.Server.Tools;

public class SubscribeWebhookInput
{
    public string EventType { get; set; } = "";
    public string WebhookUrl { get; set; } = "";
    public string? Secret { get; set; }
    public Dictionary<string, object>? Filters { get; set; }
    public WebhookRetryPolicyInput? RetryPolicy { get; set; }
}

public class WebhookRetryPolicyInput
{
    public int MaxRetries { get; set; } = 3;
    public int BackoffMs { get; set; } = 1000;
    public bool ExponentialBackoff { get; set; } = true;
}

public class SubscribeWebhookOutput
{
    public Guid SubscriptionId { get; set; }
    public string EventType { get; set; } = "";
    public string WebhookUrl { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class SubscribeWebhookInputValidator : AbstractValidator<SubscribeWebhookInput>
{
    private static readonly string[] ValidEventTypes = 
    {
        "salesorder.created",
        "salesorder.updated",
        "salesorder.cancelled",
        "customer.created",
        "customer.updated",
        "payment.posted",
        "invoice.created",
        "inventory.low_stock"
    };
    
    public SubscribeWebhookInputValidator()
    {
        RuleFor(x => x.EventType)
            .NotEmpty()
            .WithMessage("event_type is required")
            .Must(evt => ValidEventTypes.Contains(evt))
            .WithMessage($"event_type must be one of: {string.Join(", ", ValidEventTypes)}");
        
        RuleFor(x => x.WebhookUrl)
            .NotEmpty()
            .WithMessage("webhook_url is required")
            .Must(BeValidUrl)
            .WithMessage("webhook_url must be a valid HTTP/HTTPS URL");
        
        RuleFor(x => x.RetryPolicy)
            .SetValidator(new WebhookRetryPolicyInputValidator())
            .When(x => x.RetryPolicy != null);
    }
    
    private bool BeValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}

public class WebhookRetryPolicyInputValidator : AbstractValidator<WebhookRetryPolicyInput>
{
    public WebhookRetryPolicyInputValidator()
    {
        RuleFor(x => x.MaxRetries)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(10)
            .WithMessage("max_retries must be between 0 and 10");
        
        RuleFor(x => x.BackoffMs)
            .GreaterThan(0)
            .WithMessage("backoff_ms must be greater than 0");
    }
}

public class SubscribeWebhookTool : ToolBase<SubscribeWebhookInput, SubscribeWebhookOutput>
{
    private readonly IWebhookService _webhookService;
    
    public override string Name => "ax_subscribe_webhook";
    public override string Description => "Subscribe to MCP events via webhooks";
    
    public SubscribeWebhookTool(
        ILogger<SubscribeWebhookTool> logger,
        IAuditService audit,
        SubscribeWebhookInputValidator validator,
        IWebhookService webhookService)
        : base(logger, audit, validator)
    {
        _webhookService = webhookService;
    }
    
    protected override async Task<SubscribeWebhookOutput> ExecuteCoreAsync(
        SubscribeWebhookInput input,
        ToolContext context,
        CancellationToken cancellationToken)
    {
        var subscription = new WebhookSubscription
        {
            EventType = input.EventType,
            WebhookUrl = input.WebhookUrl,
            Secret = input.Secret,
            Filters = input.Filters,
            RetryPolicy = input.RetryPolicy != null
                ? new WebhookRetryPolicy
                {
                    MaxRetries = input.RetryPolicy.MaxRetries,
                    BackoffMs = input.RetryPolicy.BackoffMs,
                    ExponentialBackoff = input.RetryPolicy.ExponentialBackoff
                }
                : new WebhookRetryPolicy()
        };
        
        var result = await _webhookService.SubscribeAsync(subscription, cancellationToken);
        
        return new SubscribeWebhookOutput
        {
            SubscriptionId = result.Id,
            EventType = result.EventType,
            WebhookUrl = result.WebhookUrl,
            CreatedAt = result.CreatedAt
        };
    }
}

