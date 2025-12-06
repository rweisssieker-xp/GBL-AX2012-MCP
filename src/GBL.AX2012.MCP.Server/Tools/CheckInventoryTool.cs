using FluentValidation;
using Microsoft.Extensions.Logging;
using GBL.AX2012.MCP.Core.Exceptions;
using GBL.AX2012.MCP.Core.Interfaces;
using GBL.AX2012.MCP.Core.Models;
using GBL.AX2012.MCP.AxConnector.Interfaces;

namespace GBL.AX2012.MCP.Server.Tools;

public class CheckInventoryInput
{
    public string ItemId { get; set; } = "";
    public string? Warehouse { get; set; }
    public bool IncludeWarehouses { get; set; } = false;
}

public class CheckInventoryOutput
{
    public string ItemId { get; set; } = "";
    public string ItemName { get; set; } = "";
    public decimal TotalOnHand { get; set; }
    public decimal Available { get; set; }
    public decimal Reserved { get; set; }
    public decimal OnOrder { get; set; }
    public List<WarehouseInventoryOutput>? Warehouses { get; set; }
}

public class WarehouseInventoryOutput
{
    public string WarehouseId { get; set; } = "";
    public string WarehouseName { get; set; } = "";
    public decimal OnHand { get; set; }
    public decimal Available { get; set; }
    public decimal Reserved { get; set; }
}

public class CheckInventoryInputValidator : AbstractValidator<CheckInventoryInput>
{
    public CheckInventoryInputValidator()
    {
        RuleFor(x => x.ItemId)
            .NotEmpty()
            .WithMessage("item_id is required");
    }
}

public class CheckInventoryTool : ToolBase<CheckInventoryInput, CheckInventoryOutput>
{
    private readonly IAifClient _aifClient;
    
    public override string Name => "ax_check_inventory";
    public override string Description => "Check inventory availability for an item in AX 2012";
    
    public CheckInventoryTool(
        ILogger<CheckInventoryTool> logger,
        IAuditService audit,
        CheckInventoryInputValidator validator,
        IAifClient aifClient)
        : base(logger, audit, validator)
    {
        _aifClient = aifClient;
    }
    
    protected override async Task<CheckInventoryOutput> ExecuteCoreAsync(
        CheckInventoryInput input, 
        ToolContext context, 
        CancellationToken cancellationToken)
    {
        var inventory = await _aifClient.GetInventoryOnHandAsync(
            input.ItemId, 
            input.Warehouse, 
            cancellationToken);
        
        if (inventory.TotalOnHand == 0 && !inventory.Warehouses.Any())
        {
            var item = await _aifClient.GetItemAsync(input.ItemId, cancellationToken);
            if (item == null)
            {
                throw new AxException("ITEM_NOT_FOUND", $"Item {input.ItemId} not found");
            }
        }
        
        var output = new CheckInventoryOutput
        {
            ItemId = inventory.ItemId,
            ItemName = inventory.ItemName,
            TotalOnHand = inventory.TotalOnHand,
            Available = inventory.Available,
            Reserved = inventory.Reserved,
            OnOrder = inventory.OnOrder
        };
        
        if (input.IncludeWarehouses && inventory.Warehouses.Any())
        {
            output.Warehouses = inventory.Warehouses.Select(w => new WarehouseInventoryOutput
            {
                WarehouseId = w.WarehouseId,
                WarehouseName = w.WarehouseName,
                OnHand = w.OnHand,
                Available = w.Available,
                Reserved = w.Reserved
            }).ToList();
        }
        
        return output;
    }
}
