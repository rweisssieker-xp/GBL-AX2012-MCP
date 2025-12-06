using FluentValidation;
using Microsoft.Extensions.Logging;
using GBL.AX2012.MCP.Core.Exceptions;
using GBL.AX2012.MCP.Core.Interfaces;
using GBL.AX2012.MCP.Core.Models;
using GBL.AX2012.MCP.AxConnector.Interfaces;

namespace GBL.AX2012.MCP.Server.Tools;

public class GetItemInput
{
    public string? ItemId { get; set; }
    public string? ItemName { get; set; }
    public string? ItemGroup { get; set; }
    public bool IncludeInventory { get; set; } = false;
    public bool IncludePricing { get; set; } = false;
}

public class GetItemOutput
{
    public string ItemId { get; set; } = "";
    public string Name { get; set; } = "";
    public string ItemGroup { get; set; } = "";
    public string Unit { get; set; } = "";
    public bool BlockedForSales { get; set; }
    public bool BlockedForPurchase { get; set; }
    public string? Description { get; set; }
    public decimal? StandardCost { get; set; }
    public decimal? SalesPrice { get; set; }
    public InventoryOnHand? Inventory { get; set; }
}

public class ItemSearchOutput
{
    public List<ItemMatch> Matches { get; set; } = new();
    public int TotalCount { get; set; }
}

public class ItemMatch
{
    public string ItemId { get; set; } = "";
    public string Name { get; set; } = "";
    public string ItemGroup { get; set; } = "";
    public string Unit { get; set; } = "";
    public bool BlockedForSales { get; set; }
}

public class GetItemInputValidator : AbstractValidator<GetItemInput>
{
    public GetItemInputValidator()
    {
        RuleFor(x => x)
            .Must(x => !string.IsNullOrEmpty(x.ItemId) || !string.IsNullOrEmpty(x.ItemName) || !string.IsNullOrEmpty(x.ItemGroup))
            .WithMessage("Either item_id, item_name, or item_group is required");
    }
}

public class GetItemTool : ToolBase<GetItemInput, object>
{
    private readonly IAifClient _aifClient;
    
    public override string Name => "ax_get_item";
    public override string Description => "Get item master data by ID, name, or group";
    
    public GetItemTool(
        ILogger<GetItemTool> logger,
        IAuditService audit,
        GetItemInputValidator validator,
        IAifClient aifClient)
        : base(logger, audit, validator)
    {
        _aifClient = aifClient;
    }
    
    protected override async Task<object> ExecuteCoreAsync(
        GetItemInput input, 
        ToolContext context, 
        CancellationToken cancellationToken)
    {
        // By ID - return single item
        if (!string.IsNullOrEmpty(input.ItemId))
        {
            var item = await _aifClient.GetItemAsync(input.ItemId, cancellationToken);
            if (item == null)
            {
                throw new AxException("ITEM_NOT_FOUND", $"Item {input.ItemId} not found");
            }
            
            var output = new GetItemOutput
            {
                ItemId = item.ItemId,
                Name = item.Name,
                ItemGroup = item.ItemGroup,
                Unit = item.Unit,
                BlockedForSales = item.BlockedForSales
            };
            
            if (input.IncludeInventory)
            {
                output.Inventory = await _aifClient.GetInventoryOnHandAsync(item.ItemId, null, cancellationToken);
            }
            
            return output;
        }
        
        // By name or group - return search results
        var items = new List<Item>();
        
        if (!string.IsNullOrEmpty(input.ItemName))
        {
            // Simulated search - in real impl would call AIF
            var item = await _aifClient.GetItemAsync(input.ItemName, cancellationToken);
            if (item != null) items.Add(item);
        }
        
        return new ItemSearchOutput
        {
            Matches = items.Select(i => new ItemMatch
            {
                ItemId = i.ItemId,
                Name = i.Name,
                ItemGroup = i.ItemGroup,
                Unit = i.Unit,
                BlockedForSales = i.BlockedForSales
            }).ToList(),
            TotalCount = items.Count
        };
    }
}
