using GBL.AX2012.MCP.Core.Models;

namespace GBL.AX2012.MCP.AxConnector.Interfaces;

public interface IAifClient
{
    Task<Customer?> GetCustomerAsync(string customerAccount, CancellationToken cancellationToken = default);
    Task<IEnumerable<Customer>> SearchCustomersAsync(string searchTerm, int maxResults = 5, CancellationToken cancellationToken = default);
    Task<Item?> GetItemAsync(string itemId, CancellationToken cancellationToken = default);
    Task<SalesOrder?> GetSalesOrderAsync(string salesId, CancellationToken cancellationToken = default);
    Task<IEnumerable<SalesOrder>> GetSalesOrdersByCustomerAsync(string customerAccount, SalesOrderFilter? filter = null, CancellationToken cancellationToken = default);
    Task<InventoryOnHand> GetInventoryOnHandAsync(string itemId, string? warehouseId = null, CancellationToken cancellationToken = default);
    Task<PriceResult> SimulatePriceAsync(string customerAccount, string itemId, decimal quantity, DateTime? date = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<ReservationQueueEntry>> GetReservationQueueAsync(string itemId, string? warehouseId = null, CancellationToken cancellationToken = default);
}

public class ReservationQueueEntry
{
    public string SalesId { get; set; } = "";
    public int LineNum { get; set; }
    public string CustomerAccount { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string ItemId { get; set; } = "";
    public decimal ReservedQty { get; set; }
    public decimal PendingQty { get; set; }
    public DateTime RequestedDate { get; set; }
    public DateTime OrderDate { get; set; }
    public int Priority { get; set; }
}
