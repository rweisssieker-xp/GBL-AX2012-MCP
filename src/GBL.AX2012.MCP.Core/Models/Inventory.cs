namespace GBL.AX2012.MCP.Core.Models;

public record InventoryOnHand
{
    public string ItemId { get; init; } = "";
    public string ItemName { get; init; } = "";
    public decimal TotalOnHand { get; init; }
    public decimal Available { get; init; }
    public decimal AvailablePhysical => Available; // Alias
    public decimal Reserved { get; init; }
    public decimal OnOrder { get; init; }
    public List<WarehouseInventory> Warehouses { get; init; } = new();
}

public record WarehouseInventory
{
    public string WarehouseId { get; init; } = "";
    public string WarehouseName { get; init; } = "";
    public decimal OnHand { get; init; }
    public decimal Available { get; init; }
    public decimal Reserved { get; init; }
}
