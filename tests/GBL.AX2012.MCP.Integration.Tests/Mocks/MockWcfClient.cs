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
    
    public void Dispose()
    {
    }
}
