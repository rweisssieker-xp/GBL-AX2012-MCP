namespace GBL.AX2012.MCP.Core.Models;

public record Item
{
    public string ItemId { get; init; } = "";
    public string Name { get; init; } = "";
    public string ItemGroup { get; init; } = "";
    public string Unit { get; init; } = "PCS";
    public string UnitId => Unit; // Alias
    public bool BlockedForSales { get; init; }
    public bool Blocked => BlockedForSales; // Alias
}
