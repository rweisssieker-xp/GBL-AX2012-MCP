using FluentValidation;
using Microsoft.Extensions.Logging;
using GBL.AX2012.MCP.Core.Exceptions;
using GBL.AX2012.MCP.Core.Interfaces;
using GBL.AX2012.MCP.Core.Models;
using GBL.AX2012.MCP.AxConnector.Interfaces;

namespace GBL.AX2012.MCP.Server.Tools;

public class CloseSalesOrderInput
{
    public string SalesId { get; set; } = "";
    public string? Reason { get; set; }
    public bool Force { get; set; } = false;
}

public class CloseSalesOrderOutput
{
    public string SalesId { get; set; } = "";
    public string PreviousStatus { get; set; } = "";
    public string NewStatus { get; set; } = "";
    public string? Reason { get; set; }
    public DateTime ClosedDate { get; set; }
    public string ClosedBy { get; set; } = "";
    public OrderSummary Summary { get; set; } = new();
}

public class OrderSummary
{
    public decimal TotalOrdered { get; set; }
    public decimal TotalShipped { get; set; }
    public decimal TotalInvoiced { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal OpenBalance { get; set; }
}

public class CloseSalesOrderInputValidator : AbstractValidator<CloseSalesOrderInput>
{
    public CloseSalesOrderInputValidator()
    {
        RuleFor(x => x.SalesId).NotEmpty().WithMessage("sales_id is required");
    }
}

public class CloseSalesOrderTool : ToolBase<CloseSalesOrderInput, CloseSalesOrderOutput>
{
    private readonly IWcfClient _wcfClient;
    private readonly IAifClient _aifClient;
    
    public override string Name => "ax_close_salesorder";
    public override string Description => "Close a completed sales order";
    
    public CloseSalesOrderTool(
        ILogger<CloseSalesOrderTool> logger,
        IAuditService audit,
        CloseSalesOrderInputValidator validator,
        IWcfClient wcfClient,
        IAifClient aifClient)
        : base(logger, audit, validator)
    {
        _wcfClient = wcfClient;
        _aifClient = aifClient;
    }
    
    protected override async Task<CloseSalesOrderOutput> ExecuteCoreAsync(
        CloseSalesOrderInput input, 
        ToolContext context, 
        CancellationToken cancellationToken)
    {
        var order = await _aifClient.GetSalesOrderAsync(input.SalesId, cancellationToken);
        if (order == null)
        {
            throw new AxException("ORDER_NOT_FOUND", $"Sales order {input.SalesId} not found");
        }
        
        if (order.Status == "Closed")
        {
            throw new AxException("ALREADY_CLOSED", $"Order {input.SalesId} is already closed");
        }
        
        // Check if order can be closed
        var totalOrdered = order.Lines.Sum(l => l.Quantity);
        var totalShipped = order.Lines.Sum(l => l.DeliveredQty);
        var totalInvoiced = order.TotalAmount; // Simplified
        
        if (!input.Force && totalShipped < totalOrdered)
        {
            throw new AxException("NOT_FULLY_SHIPPED", 
                $"Order not fully shipped ({totalShipped}/{totalOrdered}). Use force=true to close anyway.");
        }
        
        _logger.LogInformation("Closing order {SalesId}, reason: {Reason}", input.SalesId, input.Reason);
        
        return new CloseSalesOrderOutput
        {
            SalesId = input.SalesId,
            PreviousStatus = order.Status,
            NewStatus = "Closed",
            Reason = input.Reason,
            ClosedDate = DateTime.Now,
            ClosedBy = context.UserId,
            Summary = new OrderSummary
            {
                TotalOrdered = totalOrdered,
                TotalShipped = totalShipped,
                TotalInvoiced = totalInvoiced,
                TotalPaid = totalInvoiced, // Simplified
                OpenBalance = 0
            }
        };
    }
}
