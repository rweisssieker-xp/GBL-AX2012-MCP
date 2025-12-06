using FluentValidation;
using Microsoft.Extensions.Logging;
using GBL.AX2012.MCP.Core.Interfaces;
using GBL.AX2012.MCP.Core.Models;
using GBL.AX2012.MCP.AxConnector.Interfaces;

namespace GBL.AX2012.MCP.Server.Tools;

public class ReleaseForPickingInput
{
    public string SalesId { get; set; } = "";
    public string? WarehouseId { get; set; }
    public bool PartialRelease { get; set; } = false;
    public List<int>? LineNumbers { get; set; } // null = all lines
}

public class ReleaseForPickingOutput
{
    public string SalesId { get; set; } = "";
    public string WarehouseId { get; set; } = "";
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public string? PickingListId { get; set; }
    public List<ReleasedLine> ReleasedLines { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

public class ReleasedLine
{
    public int LineNum { get; set; }
    public string ItemId { get; set; } = "";
    public decimal ReleasedQuantity { get; set; }
    public string PickingLocation { get; set; } = "";
}

public class ReleaseForPickingInputValidator : AbstractValidator<ReleaseForPickingInput>
{
    public ReleaseForPickingInputValidator()
    {
        RuleFor(x => x.SalesId).NotEmpty().MaximumLength(20);
        RuleFor(x => x.WarehouseId).MaximumLength(10);
    }
}

public class ReleaseForPickingTool : ToolBase<ReleaseForPickingInput, ReleaseForPickingOutput>
{
    private readonly IAifClient _aifClient;
    private readonly IWcfClient _wcfClient;
    
    public override string Name => "ax_release_for_picking";
    public override string Description => "Release a sales order for warehouse picking";
    
    public ReleaseForPickingTool(
        ILogger<ReleaseForPickingTool> logger,
        IAuditService audit,
        ReleaseForPickingInputValidator validator,
        IAifClient aifClient,
        IWcfClient wcfClient)
        : base(logger, audit, validator)
    {
        _aifClient = aifClient;
        _wcfClient = wcfClient;
    }
    
    protected override async Task<ReleaseForPickingOutput> ExecuteCoreAsync(
        ReleaseForPickingInput input, 
        ToolContext context, 
        CancellationToken cancellationToken)
    {
        // Get order
        var order = await _aifClient.GetSalesOrderAsync(input.SalesId);
        if (order == null)
        {
            return new ReleaseForPickingOutput
            {
                SalesId = input.SalesId,
                Success = false,
                Message = $"Sales order {input.SalesId} not found"
            };
        }
        
        // Check order status
        if (order.Status != "Open" && order.Status != "Confirmed")
        {
            return new ReleaseForPickingOutput
            {
                SalesId = input.SalesId,
                Success = false,
                Message = $"Order status '{order.Status}' does not allow picking release"
            };
        }
        
        var warnings = new List<string>();
        var releasedLines = new List<ReleasedLine>();
        
        // Filter lines to release
        var linesToRelease = order.Lines ?? new List<SalesLine>();
        if (input.LineNumbers != null && input.LineNumbers.Any())
        {
            linesToRelease = linesToRelease.Where(l => input.LineNumbers.Contains(l.LineNum)).ToList();
        }
        
        // Check inventory for each line
        foreach (var line in linesToRelease)
        {
            var inventory = await _aifClient.GetInventoryOnHandAsync(line.ItemId, input.WarehouseId ?? line.WarehouseId);
            var available = inventory?.AvailablePhysical ?? 0;
            
            if (available < line.Quantity)
            {
                if (input.PartialRelease && available > 0)
                {
                    warnings.Add($"Line {line.LineNum}: Partial release {available}/{line.Quantity} for {line.ItemId}");
                    releasedLines.Add(new ReleasedLine
                    {
                        LineNum = line.LineNum,
                        ItemId = line.ItemId,
                        ReleasedQuantity = available,
                        PickingLocation = $"LOC-{line.WarehouseId ?? input.WarehouseId ?? "MAIN"}-A01"
                    });
                }
                else
                {
                    warnings.Add($"Line {line.LineNum}: Insufficient stock for {line.ItemId} ({available}/{line.Quantity})");
                }
            }
            else
            {
                releasedLines.Add(new ReleasedLine
                {
                    LineNum = line.LineNum,
                    ItemId = line.ItemId,
                    ReleasedQuantity = line.Quantity,
                    PickingLocation = $"LOC-{line.WarehouseId ?? input.WarehouseId ?? "MAIN"}-A01"
                });
            }
        }
        
        if (!releasedLines.Any())
        {
            return new ReleaseForPickingOutput
            {
                SalesId = input.SalesId,
                Success = false,
                Message = "No lines could be released for picking",
                Warnings = warnings
            };
        }
        
        // Create picking list via WCF (simulated)
        var pickingListId = $"PL-{input.SalesId}-{DateTime.Now:yyyyMMddHHmmss}";
        
        return new ReleaseForPickingOutput
        {
            SalesId = input.SalesId,
            WarehouseId = input.WarehouseId ?? order.Lines?.FirstOrDefault()?.WarehouseId ?? "MAIN",
            Success = true,
            Message = $"Released {releasedLines.Count} lines for picking",
            PickingListId = pickingListId,
            ReleasedLines = releasedLines,
            Warnings = warnings
        };
    }
}
