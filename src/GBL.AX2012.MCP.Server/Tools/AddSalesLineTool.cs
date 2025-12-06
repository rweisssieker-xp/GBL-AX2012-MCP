using FluentValidation;
using Microsoft.Extensions.Logging;
using GBL.AX2012.MCP.Core.Interfaces;
using GBL.AX2012.MCP.Core.Models;
using GBL.AX2012.MCP.AxConnector.Interfaces;

namespace GBL.AX2012.MCP.Server.Tools;

public class AddSalesLineInput
{
    public string SalesId { get; set; } = "";
    public string ItemId { get; set; } = "";
    public decimal Quantity { get; set; }
    public string? UnitId { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal? DiscountPercent { get; set; }
    public string? WarehouseId { get; set; }
    public DateTime? RequestedDeliveryDate { get; set; }
}

public class AddSalesLineOutput
{
    public string SalesId { get; set; } = "";
    public int LineNum { get; set; }
    public string ItemId { get; set; } = "";
    public string ItemName { get; set; } = "";
    public decimal Quantity { get; set; }
    public string UnitId { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public decimal LineAmount { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal NetAmount { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public decimal NewOrderTotal { get; set; }
}

public class AddSalesLineInputValidator : AbstractValidator<AddSalesLineInput>
{
    public AddSalesLineInputValidator()
    {
        RuleFor(x => x.SalesId).NotEmpty().MaximumLength(20);
        RuleFor(x => x.ItemId).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0).When(x => x.UnitPrice.HasValue);
        RuleFor(x => x.DiscountPercent).InclusiveBetween(0, 100).When(x => x.DiscountPercent.HasValue);
    }
}

public class AddSalesLineTool : ToolBase<AddSalesLineInput, AddSalesLineOutput>
{
    private readonly IAifClient _aifClient;
    private readonly IWcfClient _wcfClient;
    
    public override string Name => "ax_add_salesline";
    public override string Description => "Add a new line to an existing sales order";
    
    public AddSalesLineTool(
        ILogger<AddSalesLineTool> logger,
        IAuditService audit,
        AddSalesLineInputValidator validator,
        IAifClient aifClient,
        IWcfClient wcfClient)
        : base(logger, audit, validator)
    {
        _aifClient = aifClient;
        _wcfClient = wcfClient;
    }
    
    protected override async Task<AddSalesLineOutput> ExecuteCoreAsync(
        AddSalesLineInput input, 
        ToolContext context, 
        CancellationToken cancellationToken)
    {
        // Get current order
        var order = await _aifClient.GetSalesOrderAsync(input.SalesId);
        if (order == null)
        {
            return new AddSalesLineOutput
            {
                SalesId = input.SalesId,
                Success = false,
                Message = $"Sales order {input.SalesId} not found"
            };
        }
        
        // Check if order can be modified
        if (order.Status == "Invoiced" || order.Status == "Cancelled")
        {
            return new AddSalesLineOutput
            {
                SalesId = input.SalesId,
                Success = false,
                Message = $"Cannot add lines to order in status '{order.Status}'"
            };
        }
        
        // Validate item exists and is not blocked
        var item = await _aifClient.GetItemAsync(input.ItemId);
        if (item == null)
        {
            return new AddSalesLineOutput
            {
                SalesId = input.SalesId,
                ItemId = input.ItemId,
                Success = false,
                Message = $"Item {input.ItemId} not found"
            };
        }
        
        if (item.Blocked)
        {
            return new AddSalesLineOutput
            {
                SalesId = input.SalesId,
                ItemId = input.ItemId,
                Success = false,
                Message = $"Item {input.ItemId} is blocked for sales"
            };
        }
        
        // Get price if not specified
        var unitPrice = input.UnitPrice ?? 0;
        if (!input.UnitPrice.HasValue)
        {
            var priceResult = await _aifClient.SimulatePriceAsync(order.CustomerAccount, input.ItemId, input.Quantity);
            unitPrice = priceResult?.UnitPrice ?? 0;
        }
        
        // Calculate next line number
        var nextLineNum = (order.Lines?.Max(l => l.LineNum) ?? 0) + 1;
        
        // Create line via WCF
        var lineRequest = new SalesLineCreateRequest
        {
            SalesId = input.SalesId,
            ItemId = input.ItemId,
            Quantity = input.Quantity,
            UnitId = input.UnitId ?? item.UnitId,
            UnitPrice = unitPrice,
            DiscountPercent = input.DiscountPercent ?? 0,
            WarehouseId = input.WarehouseId,
            RequestedDeliveryDate = input.RequestedDeliveryDate ?? order.RequestedDeliveryDate
        };
        
        var lineNum = await _wcfClient.AddSalesLineAsync(lineRequest);
        var success = lineNum > 0;
        
        // Calculate amounts
        var lineAmount = input.Quantity * unitPrice;
        var discountAmount = lineAmount * ((input.DiscountPercent ?? 0) / 100);
        var netAmount = lineAmount - discountAmount;
        
        // Calculate new order total
        var existingTotal = order.Lines?.Sum(l => l.NetAmount) ?? 0;
        var newOrderTotal = existingTotal + netAmount;
        
        return new AddSalesLineOutput
        {
            SalesId = input.SalesId,
            LineNum = success ? lineNum : 0,
            ItemId = input.ItemId,
            ItemName = item.Name,
            Quantity = input.Quantity,
            UnitId = input.UnitId ?? item.UnitId,
            UnitPrice = unitPrice,
            LineAmount = lineAmount,
            DiscountPercent = input.DiscountPercent ?? 0,
            NetAmount = netAmount,
            Success = success,
            Message = success 
                ? $"Line {lineNum} added to order {input.SalesId}: {input.Quantity} x {item.Name}"
                : "Failed to add sales line",
            NewOrderTotal = newOrderTotal
        };
    }
}
