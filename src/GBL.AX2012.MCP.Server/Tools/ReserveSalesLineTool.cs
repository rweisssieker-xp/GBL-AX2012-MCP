using FluentValidation;
using Microsoft.Extensions.Logging;
using GBL.AX2012.MCP.Core.Exceptions;
using GBL.AX2012.MCP.Core.Interfaces;
using GBL.AX2012.MCP.Core.Models;
using GBL.AX2012.MCP.AxConnector.Interfaces;

namespace GBL.AX2012.MCP.Server.Tools;

public class ReserveSalesLineInput
{
    public string SalesId { get; set; } = "";
    public int LineNum { get; set; }
    public decimal? Quantity { get; set; }
    public string? Warehouse { get; set; }
}

public class ReserveSalesLineOutput
{
    public string SalesId { get; set; } = "";
    public int LineNum { get; set; }
    public string ItemId { get; set; } = "";
    public decimal RequestedQty { get; set; }
    public decimal ReservedQty { get; set; }
    public decimal PreviouslyReserved { get; set; }
    public bool FullyReserved { get; set; }
    public string Warehouse { get; set; } = "";
    public List<string> Warnings { get; set; } = new();
}

public class ReserveSalesLineInputValidator : AbstractValidator<ReserveSalesLineInput>
{
    public ReserveSalesLineInputValidator()
    {
        RuleFor(x => x.SalesId).NotEmpty().WithMessage("sales_id is required");
        RuleFor(x => x.LineNum).GreaterThan(0).WithMessage("line_num must be greater than 0");
    }
}

public class ReserveSalesLineTool : ToolBase<ReserveSalesLineInput, ReserveSalesLineOutput>
{
    private readonly IWcfClient _wcfClient;
    private readonly IAifClient _aifClient;
    
    public override string Name => "ax_reserve_salesline";
    public override string Description => "Reserve inventory for a sales order line";
    
    public ReserveSalesLineTool(
        ILogger<ReserveSalesLineTool> logger,
        IAuditService audit,
        ReserveSalesLineInputValidator validator,
        IWcfClient wcfClient,
        IAifClient aifClient)
        : base(logger, audit, validator)
    {
        _wcfClient = wcfClient;
        _aifClient = aifClient;
    }
    
    protected override async Task<ReserveSalesLineOutput> ExecuteCoreAsync(
        ReserveSalesLineInput input, 
        ToolContext context, 
        CancellationToken cancellationToken)
    {
        // Get order and line
        var order = await _aifClient.GetSalesOrderAsync(input.SalesId, cancellationToken);
        if (order == null)
        {
            throw new AxException("ORDER_NOT_FOUND", $"Sales order {input.SalesId} not found");
        }
        
        var line = order.Lines.FirstOrDefault(l => l.LineNum == input.LineNum);
        if (line == null)
        {
            throw new AxException("LINE_NOT_FOUND", $"Line {input.LineNum} not found on order {input.SalesId}");
        }
        
        var qtyToReserve = input.Quantity ?? (line.Quantity - line.ReservedQty);
        var warehouse = input.Warehouse ?? "WH-MAIN";
        var warnings = new List<string>();
        
        // Check inventory
        var inventory = await _aifClient.GetInventoryOnHandAsync(line.ItemId, warehouse, cancellationToken);
        if (inventory.Available < qtyToReserve)
        {
            warnings.Add($"Requested {qtyToReserve}, only {inventory.Available} available in {warehouse}");
            qtyToReserve = inventory.Available;
        }
        
        // Reserve via WCF (simulated - would call actual WCF method)
        _logger.LogInformation("Reserving {Qty} of {Item} for {SalesId}/{LineNum}", 
            qtyToReserve, line.ItemId, input.SalesId, input.LineNum);
        
        var totalReserved = line.ReservedQty + qtyToReserve;
        
        return new ReserveSalesLineOutput
        {
            SalesId = input.SalesId,
            LineNum = input.LineNum,
            ItemId = line.ItemId,
            RequestedQty = input.Quantity ?? (line.Quantity - line.ReservedQty),
            ReservedQty = qtyToReserve,
            PreviouslyReserved = line.ReservedQty,
            FullyReserved = totalReserved >= line.Quantity,
            Warehouse = warehouse,
            Warnings = warnings
        };
    }
}
