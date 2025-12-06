namespace GBL.AX2012.MCP.Core.Models;

public record SalesOrder
{
    public string SalesId { get; init; } = "";
    public string CustomerAccount { get; init; } = "";
    public string CustomerName { get; init; } = "";
    public string? CustomerRef { get; init; }
    public DateTime OrderDate { get; init; }
    public DateTime RequestedDelivery { get; init; }
    public DateTime RequestedDeliveryDate => RequestedDelivery; // Alias for compatibility
    public string Status { get; init; } = "";
    public decimal TotalAmount { get; init; }
    public string Currency { get; init; } = "EUR";
    public List<SalesLine> Lines { get; init; } = new();
}

public record SalesLine
{
    public int LineNum { get; init; }
    public string ItemId { get; init; } = "";
    public string ItemName { get; init; } = "";
    public decimal Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal LineAmount { get; init; }
    public decimal NetAmount => LineAmount; // Alias
    public decimal ReservedQty { get; init; }
    public decimal DeliveredQty { get; init; }
    public string? WarehouseId { get; init; }
}

public class SalesOrderFilter
{
    public string[]? StatusFilter { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 20;
}
