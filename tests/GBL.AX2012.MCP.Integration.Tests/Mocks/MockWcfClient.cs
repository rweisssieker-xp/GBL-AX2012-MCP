using GBL.AX2012.MCP.AxConnector.Interfaces;

namespace GBL.AX2012.MCP.Integration.Tests.Mocks;

public class MockWcfClient : IWcfClient
{
    private int _orderCounter = 1000;
    private readonly List<string> _createdOrders = new();
    private readonly List<string> _updatedOrders = new();
    
    public List<string> CreatedOrders => _createdOrders;
    public List<string> UpdatedOrders => _updatedOrders;
    
    public Task<string> CreateSalesOrderAsync(CreateSalesOrderRequest request, CancellationToken cancellationToken = default)
    {
        var salesId = $"SO-2024-{++_orderCounter}";
        _createdOrders.Add(salesId);
        return Task.FromResult(salesId);
    }
    
    public Task<bool> UpdateSalesOrderAsync(UpdateSalesOrderRequest request, CancellationToken cancellationToken = default)
    {
        _updatedOrders.Add(request.SalesId);
        return Task.FromResult(true);
    }
    
    public Task<int> AddSalesLineAsync(SalesLineCreateRequest request, CancellationToken cancellationToken = default)
    {
        // Return next line number
        return Task.FromResult(10);
    }
    
    public Task<bool> SendOrderConfirmationAsync(SendOrderConfirmationRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }
    
    public Task<SplitOrderResult> SplitOrderByCreditAsync(SplitOrderRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new SplitOrderResult
        {
            WasSplit = true,
            OriginalSalesId = request.SalesId,
            NewSalesId = $"{request.SalesId}-SPLIT",
            OriginalOrderAmount = 100000m,
            SplitAmount = 50000m,
            SplitLines = new List<SplitLineInfo>
            {
                new SplitLineInfo
                {
                    OriginalLineNum = 1,
                    NewLineNum = 1,
                    ItemId = "ITEM-001",
                    OriginalQty = 100,
                    RemainingQty = 50,
                    SplitQty = 50
                }
            }
        });
    }
    
    public void Dispose()
    {
    }
}
