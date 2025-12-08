using FluentValidation;
using Microsoft.Extensions.Logging;
using GBL.AX2012.MCP.Core.Interfaces;
using GBL.AX2012.MCP.Core.Models;
using GBL.AX2012.MCP.Server.Webhooks;

namespace GBL.AX2012.MCP.Server.Tools;

public class UnsubscribeWebhookInput
{
    public Guid SubscriptionId { get; set; }
}

public class UnsubscribeWebhookOutput
{
    public Guid SubscriptionId { get; set; }
    public bool Success { get; set; }
}

public class UnsubscribeWebhookInputValidator : AbstractValidator<UnsubscribeWebhookInput>
{
    public UnsubscribeWebhookInputValidator()
    {
        RuleFor(x => x.SubscriptionId)
            .NotEmpty()
            .WithMessage("subscription_id is required");
    }
}

public class UnsubscribeWebhookTool : ToolBase<UnsubscribeWebhookInput, UnsubscribeWebhookOutput>
{
    private readonly IWebhookService _webhookService;
    
    public override string Name => "ax_unsubscribe_webhook";
    public override string Description => "Unsubscribe from a webhook";
    
    public UnsubscribeWebhookTool(
        ILogger<UnsubscribeWebhookTool> logger,
        IAuditService audit,
        UnsubscribeWebhookInputValidator validator,
        IWebhookService webhookService)
        : base(logger, audit, validator)
    {
        _webhookService = webhookService;
    }
    
    protected override async Task<UnsubscribeWebhookOutput> ExecuteCoreAsync(
        UnsubscribeWebhookInput input,
        ToolContext context,
        CancellationToken cancellationToken)
    {
        await _webhookService.UnsubscribeAsync(input.SubscriptionId, cancellationToken);
        
        return new UnsubscribeWebhookOutput
        {
            SubscriptionId = input.SubscriptionId,
            Success = true
        };
    }
}

