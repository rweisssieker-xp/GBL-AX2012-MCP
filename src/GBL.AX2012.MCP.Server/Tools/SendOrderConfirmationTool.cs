using FluentValidation;
using Microsoft.Extensions.Logging;
using GBL.AX2012.MCP.Core.Interfaces;
using GBL.AX2012.MCP.Core.Models;
using GBL.AX2012.MCP.AxConnector.Interfaces;

namespace GBL.AX2012.MCP.Server.Tools;

public class SendOrderConfirmationInput
{
    public string SalesId { get; set; } = "";
    public string? EmailOverride { get; set; }
    public bool IncludePrices { get; set; } = true;
    public string? Language { get; set; }
}

public class SendOrderConfirmationOutput
{
    public bool Success { get; set; }
    public string SalesId { get; set; } = "";
    public string? SentTo { get; set; }
    public DateTime? SentAt { get; set; }
    public string? Message { get; set; }
}

public class SendOrderConfirmationInputValidator : AbstractValidator<SendOrderConfirmationInput>
{
    public SendOrderConfirmationInputValidator()
    {
        RuleFor(x => x.SalesId)
            .NotEmpty()
            .WithMessage("SalesId is required");
            
        RuleFor(x => x.EmailOverride)
            .EmailAddress()
            .When(x => !string.IsNullOrEmpty(x.EmailOverride))
            .WithMessage("EmailOverride must be a valid email address");
    }
}

public class SendOrderConfirmationTool : ToolBase<SendOrderConfirmationInput, SendOrderConfirmationOutput>
{
    private readonly IWcfClient _wcfClient;
    private readonly IAifClient _aifClient;
    private readonly SendOrderConfirmationInputValidator _validator;
    
    public override string Name => "ax_send_order_confirmation";
    public override string Description => "Send order confirmation email to customer";
    
    public SendOrderConfirmationTool(
        ILogger<SendOrderConfirmationTool> logger,
        IAuditService auditService,
        IWcfClient wcfClient,
        IAifClient aifClient,
        SendOrderConfirmationInputValidator validator)
        : base(logger, auditService)
    {
        _wcfClient = wcfClient;
        _aifClient = aifClient;
        _validator = validator;
    }
    
    protected override async Task<SendOrderConfirmationOutput> ExecuteCoreAsync(
        SendOrderConfirmationInput input,
        ToolContext context,
        CancellationToken cancellationToken)
    {
        var validation = await _validator.ValidateAsync(input, cancellationToken);
        if (!validation.IsValid)
        {
            throw new FluentValidation.ValidationException(validation.Errors);
        }
        
        _logger.LogInformation("Sending order confirmation for {SalesId}", input.SalesId);
        
        // Verify order exists
        var order = await _aifClient.GetSalesOrderAsync(input.SalesId, cancellationToken);
        if (order == null)
        {
            return new SendOrderConfirmationOutput
            {
                Success = false,
                SalesId = input.SalesId,
                Message = $"Sales order {input.SalesId} not found"
            };
        }
        
        // Send confirmation
        var request = new SendOrderConfirmationRequest
        {
            SalesId = input.SalesId,
            EmailOverride = input.EmailOverride,
            IncludePrices = input.IncludePrices,
            Language = input.Language
        };
        
        await _wcfClient.SendOrderConfirmationAsync(request, cancellationToken);
        
        _logger.LogInformation("Order confirmation sent for {SalesId}", input.SalesId);
        
        return new SendOrderConfirmationOutput
        {
            Success = true,
            SalesId = input.SalesId,
            SentTo = input.EmailOverride ?? order.CustomerName,
            SentAt = DateTime.UtcNow,
            Message = "Order confirmation sent successfully"
        };
    }
}
