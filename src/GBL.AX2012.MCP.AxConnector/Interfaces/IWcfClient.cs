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

public class SendOrderConfirmationRequest
{
    public string SalesId { get; set; } = "";
    public string? EmailOverride { get; set; }
    public bool IncludePrices { get; set; } = true;
    public string? Language { get; set; }
}

public class SplitOrderRequest
{
    public string SalesId { get; set; } = "";
    public decimal CreditLimit { get; set; }
    public decimal CurrentBalance { get; set; }
}

public class SplitOrderResult
{
    public bool WasSplit { get; set; }
    public string OriginalSalesId { get; set; } = "";
    public string? NewSalesId { get; set; }
    public decimal OriginalOrderAmount { get; set; }
    public decimal? SplitAmount { get; set; }
    public List<SplitLineInfo> SplitLines { get; set; } = new();
}

public class SplitLineInfo
{
    public int OriginalLineNum { get; set; }
    public int? NewLineNum { get; set; }
    public string ItemId { get; set; } = "";
    public decimal OriginalQty { get; set; }
    public decimal RemainingQty { get; set; }
    public decimal SplitQty { get; set; }
}
