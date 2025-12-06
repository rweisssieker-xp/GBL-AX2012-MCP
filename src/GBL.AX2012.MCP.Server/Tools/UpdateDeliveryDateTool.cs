using FluentValidation;
using Microsoft.Extensions.Logging;
using GBL.AX2012.MCP.Core.Interfaces;
using GBL.AX2012.MCP.Core.Models;
using GBL.AX2012.MCP.AxConnector.Interfaces;
using UpdateSalesOrderRequest = GBL.AX2012.MCP.AxConnector.Interfaces.UpdateSalesOrderRequest;

namespace GBL.AX2012.MCP.Server.Tools;

public class UpdateDeliveryDateInput
{
    public string SalesId { get; set; } = "";
    public DateTime NewDeliveryDate { get; set; }
    public string? Reason { get; set; }
    public bool NotifyCustomer { get; set; } = false;
}

public class UpdateDeliveryDateOutput
{
    public string SalesId { get; set; } = "";
    public DateTime OldDeliveryDate { get; set; }
    public DateTime NewDeliveryDate { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public bool CustomerNotified { get; set; }
    public List<string> AffectedLines { get; set; } = new();
}

public class UpdateDeliveryDateInputValidator : AbstractValidator<UpdateDeliveryDateInput>
{
    public UpdateDeliveryDateInputValidator()
    {
        RuleFor(x => x.SalesId).NotEmpty().MaximumLength(20);
        RuleFor(x => x.NewDeliveryDate).GreaterThan(DateTime.Today.AddDays(-1))
            .WithMessage("Delivery date cannot be in the past");
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}

public class UpdateDeliveryDateTool : ToolBase<UpdateDeliveryDateInput, UpdateDeliveryDateOutput>
{
    private readonly IAifClient _aifClient;
    private readonly IWcfClient _wcfClient;
    
    public override string Name => "ax_update_delivery_date";
    public override string Description => "Update the delivery date for a sales order";
    
    public UpdateDeliveryDateTool(
        ILogger<UpdateDeliveryDateTool> logger,
        IAuditService audit,
        UpdateDeliveryDateInputValidator validator,
        IAifClient aifClient,
        IWcfClient wcfClient)
        : base(logger, audit, validator)
    {
        _aifClient = aifClient;
        _wcfClient = wcfClient;
    }
    
    protected override async Task<UpdateDeliveryDateOutput> ExecuteCoreAsync(
        UpdateDeliveryDateInput input, 
        ToolContext context, 
        CancellationToken cancellationToken)
    {
        // Get current order
        var order = await _aifClient.GetSalesOrderAsync(input.SalesId);
        if (order == null)
        {
            return new UpdateDeliveryDateOutput
            {
                SalesId = input.SalesId,
                Success = false,
                Message = $"Sales order {input.SalesId} not found"
            };
        }
        
        var oldDeliveryDate = order.RequestedDeliveryDate;
        
        // Check if order can be modified
        if (order.Status == "Invoiced" || order.Status == "Cancelled")
        {
            return new UpdateDeliveryDateOutput
            {
                SalesId = input.SalesId,
                OldDeliveryDate = oldDeliveryDate,
                NewDeliveryDate = input.NewDeliveryDate,
                Success = false,
                Message = $"Cannot modify delivery date for order in status '{order.Status}'"
            };
        }
        
        // Update delivery date via WCF
        var updateRequest = new UpdateSalesOrderRequest
        {
            SalesId = input.SalesId,
            RequestedDeliveryDate = input.NewDeliveryDate
        };
        
        var success = await _wcfClient.UpdateSalesOrderAsync(updateRequest);
        
        // Get affected lines
        var affectedLines = order.Lines?.Select(l => $"Line {l.LineNum}: {l.ItemId}").ToList() ?? new List<string>();
        
        // Notify customer if requested (simulated)
        var customerNotified = false;
        if (success && input.NotifyCustomer)
        {
            customerNotified = await NotifyCustomerAsync(order.CustomerAccount, input.SalesId, oldDeliveryDate, input.NewDeliveryDate, input.Reason);
        }
        
        return new UpdateDeliveryDateOutput
        {
            SalesId = input.SalesId,
            OldDeliveryDate = oldDeliveryDate,
            NewDeliveryDate = input.NewDeliveryDate,
            Success = success,
            Message = success 
                ? $"Delivery date updated from {oldDeliveryDate:yyyy-MM-dd} to {input.NewDeliveryDate:yyyy-MM-dd}"
                : "Failed to update delivery date",
            CustomerNotified = customerNotified,
            AffectedLines = affectedLines
        };
    }
    
    private Task<bool> NotifyCustomerAsync(string customerId, string salesId, DateTime oldDate, DateTime newDate, string? reason)
    {
        // Simulated - in real implementation, send email via AX or external service
        _logger.LogInformation("Customer {CustomerId} notified about delivery date change for {SalesId}: {OldDate} -> {NewDate}. Reason: {Reason}",
            customerId, salesId, oldDate, newDate, reason ?? "Not specified");
        return Task.FromResult(true);
    }
}
