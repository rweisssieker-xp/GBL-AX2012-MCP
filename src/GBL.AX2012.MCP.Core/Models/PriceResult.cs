namespace GBL.AX2012.MCP.Core.Models;

public record PriceResult
{
    public decimal BasePrice { get; init; }
    public decimal CustomerDiscountPct { get; init; }
    public decimal QuantityDiscountPct { get; init; }
    public decimal FinalUnitPrice { get; init; }
    public decimal LineAmount { get; init; }
    public string Currency { get; init; } = "EUR";
    public string PriceSource { get; init; } = "";
    public DateTime? ValidUntil { get; init; }
}
