using FluentValidation;
using Microsoft.Extensions.Logging;
using GBL.AX2012.MCP.Core.Exceptions;
using GBL.AX2012.MCP.Core.Interfaces;
using GBL.AX2012.MCP.Core.Models;
using GBL.AX2012.MCP.AxConnector.Interfaces;

namespace GBL.AX2012.MCP.Server.Tools;

public class PostShipmentInput
{
    public string SalesId { get; set; } = "";
    public string? PackingSlipId { get; set; }
    public DateTime? ShipDate { get; set; }
    public string? TrackingNumber { get; set; }
    public string? Carrier { get; set; }
    public List<ShipmentLineInput>? Lines { get; set; }
}

public class ShipmentLineInput
{
    public int LineNum { get; set; }
    public decimal Quantity { get; set; }
}

public class PostShipmentOutput
{
    public string SalesId { get; set; } = "";
    public string PackingSlipId { get; set; } = "";
    public DateTime ShipDate { get; set; }
    public string? TrackingNumber { get; set; }
    public string? Carrier { get; set; }
    public int LinesShipped { get; set; }
    public decimal TotalQuantity { get; set; }
    public string Status { get; set; } = "";
}

public class PostShipmentInputValidator : AbstractValidator<PostShipmentInput>
{
    public PostShipmentInputValidator()
    {
        RuleFor(x => x.SalesId).NotEmpty().WithMessage("sales_id is required");
    }
}

public class PostShipmentTool : ToolBase<PostShipmentInput, PostShipmentOutput>
{
    private readonly IWcfClient _wcfClient;
    private readonly IAifClient _aifClient;
    
    public override string Name => "ax_post_shipment";
    public override string Description => "Post a shipment/packing slip for a sales order";
    
    public PostShipmentTool(
        ILogger<PostShipmentTool> logger,
        IAuditService audit,
        PostShipmentInputValidator validator,
        IWcfClient wcfClient,
        IAifClient aifClient)
        : base(logger, audit, validator)
    {
        _wcfClient = wcfClient;
        _aifClient = aifClient;
    }
    
    protected override async Task<PostShipmentOutput> ExecuteCoreAsync(
        PostShipmentInput input, 
        ToolContext context, 
        CancellationToken cancellationToken)
    {
        var order = await _aifClient.GetSalesOrderAsync(input.SalesId, cancellationToken);
        if (order == null)
        {
            throw new AxException("ORDER_NOT_FOUND", $"Sales order {input.SalesId} not found");
        }
        
        if (order.Status == "Cancelled")
        {
            throw new AxException("ORDER_CANCELLED", $"Cannot ship cancelled order {input.SalesId}");
        }
        
        var linesToShip = input.Lines ?? order.Lines
            .Where(l => l.ReservedQty > l.DeliveredQty)
            .Select(l => new ShipmentLineInput 
            { 
                LineNum = l.LineNum, 
                Quantity = l.ReservedQty - l.DeliveredQty 
            })
            .ToList();
        
        if (!linesToShip.Any())
        {
            throw new AxException("NOTHING_TO_SHIP", "No lines available to ship");
        }
        
        var packingSlipId = input.PackingSlipId ?? $"PS-{input.SalesId}-{DateTime.Now:yyyyMMddHHmmss}";
        var shipDate = input.ShipDate ?? DateTime.Today;
        
        _logger.LogInformation("Posting shipment {PackingSlip} for order {SalesId}", packingSlipId, input.SalesId);
        
        return new PostShipmentOutput
        {
            SalesId = input.SalesId,
            PackingSlipId = packingSlipId,
            ShipDate = shipDate,
            TrackingNumber = input.TrackingNumber,
            Carrier = input.Carrier,
            LinesShipped = linesToShip.Count,
            TotalQuantity = linesToShip.Sum(l => l.Quantity),
            Status = "Shipped"
        };
    }
}
