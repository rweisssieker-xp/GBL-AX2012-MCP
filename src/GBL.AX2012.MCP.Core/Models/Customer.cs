namespace GBL.AX2012.MCP.Core.Models;

public record Customer
{
    public string AccountNum { get; init; } = "";
    public string Name { get; init; } = "";
    public string Currency { get; init; } = "EUR";
    public decimal CreditLimit { get; init; }
    public decimal CreditUsed { get; init; }
    public decimal Balance => CreditUsed; // Alias for compatibility
    public string PaymentTerms { get; init; } = "";
    public string PriceGroup { get; init; } = "";
    public bool Blocked { get; init; }
    public int MatchConfidence { get; init; } = 100;
}
