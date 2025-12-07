namespace GBL.AX2012.MCP.AxConnector.Interfaces;

public interface IWcfClient
{
    Task<string> CreateSalesOrderAsync(CreateSalesOrderRequest request, CancellationToken cancellationToken = default);
    Task<bool> UpdateSalesOrderAsync(UpdateSalesOrderRequest request, CancellationToken cancellationToken = default);
    Task<int> AddSalesLineAsync(SalesLineCreateRequest request, CancellationToken cancellationToken = default);
    Task<bool> SendOrderConfirmationAsync(SendOrderConfirmationRequest request, CancellationToken cancellationToken = default);
    Task<SplitOrderResult> SplitOrderByCreditAsync(SplitOrderRequest request, CancellationToken cancellationToken = default);
}

public class CreateSalesOrderRequest
{
    public string CustomerAccount { get; set; } = "";
    public DateTime RequestedDeliveryDate { get; set; }
    public string? CustomerRef { get; set; }
    public List<CreateSalesLineRequest> Lines { get; set; } = new();
}

public class CreateSalesLineRequest
{
    public string ItemId { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal? UnitPrice { get; set; }
    public string? WarehouseId { get; set; }
}

public class UpdateSalesOrderRequest
{
    public string SalesId { get; set; } = "";
    public DateTime? RequestedDeliveryDate { get; set; }
    public string? CustomerRef { get; set; }
}

public class SalesLineCreateRequest
{
    public string SalesId { get; set; } = "";
    public string ItemId { get; set; } = "";
    public decimal Quantity { get; set; }
    public string? UnitId { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public string? WarehouseId { get; set; }
    public DateTime? RequestedDeliveryDate { get; set; }
}
