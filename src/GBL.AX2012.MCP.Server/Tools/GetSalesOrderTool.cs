using FluentValidation;
using Microsoft.Extensions.Logging;
using GBL.AX2012.MCP.Core.Exceptions;
using GBL.AX2012.MCP.Core.Interfaces;
using GBL.AX2012.MCP.Core.Models;
using GBL.AX2012.MCP.AxConnector.Interfaces;

namespace GBL.AX2012.MCP.Server.Tools;

public class GetSalesOrderInput
{
    public string? SalesId { get; set; }
    public string? CustomerAccount { get; set; }
    public string[]? StatusFilter { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public bool IncludeLines { get; set; } = false;
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 20;
}

public class GetSalesOrderOutput
{
    public string SalesId { get; set; } = "";
    public string CustomerAccount { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public DateTime OrderDate { get; set; }
    public DateTime RequestedDelivery { get; set; }
    public string Status { get; set; } = "";
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "";
    public List<SalesLineOutput>? Lines { get; set; }
}

public class SalesLineOutput
{
    public int LineNum { get; set; }
    public string ItemId { get; set; } = "";
    public string ItemName { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineAmount { get; set; }
    public decimal ReservedQty { get; set; }
    public decimal DeliveredQty { get; set; }
    public string Status { get; set; } = "";
}

public class GetSalesOrderListOutput
{
    public List<GetSalesOrderOutput> Orders { get; set; } = new();
    public int Skip { get; set; }
    public int Take { get; set; }
    public bool HasMore { get; set; }
}

public class GetSalesOrderInputValidator : AbstractValidator<GetSalesOrderInput>
{
    public GetSalesOrderInputValidator()
    {
        RuleFor(x => x)
            .Must(x => !string.IsNullOrEmpty(x.SalesId) || !string.IsNullOrEmpty(x.CustomerAccount))
            .WithMessage("Either sales_id or customer_account must be provided");
    }
}

public class GetSalesOrderTool : ToolBase<GetSalesOrderInput, object>
{
    private readonly IAifClient _aifClient;
    
    public override string Name => "ax_get_salesorder";
    public override string Description => "Retrieve sales order information from AX 2012";
    
    public GetSalesOrderTool(
        ILogger<GetSalesOrderTool> logger,
        IAuditService audit,
        GetSalesOrderInputValidator validator,
        IAifClient aifClient)
        : base(logger, audit, validator)
    {
        _aifClient = aifClient;
    }
    
    protected override async Task<object> ExecuteCoreAsync(
        GetSalesOrderInput input, 
        ToolContext context, 
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(input.SalesId))
        {
            var order = await _aifClient.GetSalesOrderAsync(input.SalesId, cancellationToken);
            
            if (order == null)
            {
                throw new AxException("ORDER_NOT_FOUND", $"Sales order {input.SalesId} not found");
            }
            
            return MapToOutput(order, input.IncludeLines);
        }
        else if (!string.IsNullOrEmpty(input.CustomerAccount))
        {
            var filter = new SalesOrderFilter
            {
                StatusFilter = input.StatusFilter,
                DateFrom = input.DateFrom,
                DateTo = input.DateTo,
                Skip = input.Skip,
                Take = input.Take
            };
            
            var orders = await _aifClient.GetSalesOrdersByCustomerAsync(
                input.CustomerAccount, filter, cancellationToken);
            
            var orderList = orders.ToList();
            
            return new GetSalesOrderListOutput
            {
                Orders = orderList.Select(o => MapToOutput(o, input.IncludeLines)).ToList(),
                Skip = input.Skip,
                Take = input.Take,
                HasMore = orderList.Count == input.Take
            };
        }
        
        throw new AxException("INVALID_INPUT", "Either sales_id or customer_account must be provided");
    }
    
    private GetSalesOrderOutput MapToOutput(SalesOrder order, bool includeLines)
    {
        var output = new GetSalesOrderOutput
        {
            SalesId = order.SalesId,
            CustomerAccount = order.CustomerAccount,
            CustomerName = order.CustomerName,
            OrderDate = order.OrderDate,
            RequestedDelivery = order.RequestedDelivery,
            Status = order.Status,
            TotalAmount = order.TotalAmount,
            Currency = order.Currency
        };
        
        if (includeLines && order.Lines.Any())
        {
            output.Lines = order.Lines.Select(l => new SalesLineOutput
            {
                LineNum = l.LineNum,
                ItemId = l.ItemId,
                ItemName = l.ItemName,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                LineAmount = l.LineAmount,
                ReservedQty = l.ReservedQty,
                DeliveredQty = l.DeliveredQty,
                Status = CalculateLineStatus(l)
            }).ToList();
        }
        
        return output;
    }
    
    private string CalculateLineStatus(SalesLine line)
    {
        if (line.DeliveredQty >= line.Quantity) return "Delivered";
        if (line.DeliveredQty > 0) return "Partially Delivered";
        if (line.ReservedQty >= line.Quantity) return "Reserved";
        if (line.ReservedQty > 0) return "Partially Reserved";
        return "Open";
    }
}
