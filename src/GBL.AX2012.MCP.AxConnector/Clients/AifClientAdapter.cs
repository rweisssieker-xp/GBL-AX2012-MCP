using System.Net.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using GBL.AX2012.MCP.Core.Exceptions;
using GBL.AX2012.MCP.Core.Interfaces;
using GBL.AX2012.MCP.Core.Models;
using GBL.AX2012.MCP.Core.Options;
using GBL.AX2012.MCP.AxConnector.Interfaces;

namespace GBL.AX2012.MCP.AxConnector.Clients;

/// <summary>
/// Adapter that automatically falls back between HTTP and NetTcp AIF clients
/// </summary>
public class AifClientAdapter : IAifClient
{
    private readonly IAifClient _httpClient;
    private readonly IAifClient _netTcpClient;
    private readonly AifClientOptions _options;
    private readonly ILogger<AifClientAdapter> _logger;
    private bool _preferNetTcp = false;
    
    public AifClientAdapter(
        AifClient httpClient,
        AifNetTcpClient netTcpClient,
        IOptions<AifClientOptions> options,
        ILogger<AifClientAdapter> logger)
    {
        _httpClient = httpClient;
        _netTcpClient = netTcpClient;
        _options = options.Value;
        _logger = logger;
        
        // Determine preferred client based on configuration
        _preferNetTcp = _options.FallbackStrategy.ToLowerInvariant() switch
        {
            "nettcp" => true,
            "http" => false,
            "auto" => false, // Start with HTTP, fallback to NetTcp
            _ => false
        };
    }
    
    private async Task<T> ExecuteWithFallbackAsync<T>(Func<IAifClient, Task<T>> operation, string operationName)
    {
        // If explicitly configured to use NetTcp, use it directly
        if (_options.UseNetTcp || _preferNetTcp)
        {
            try
            {
                _logger.LogDebug("Using NetTcp client for {Operation}", operationName);
                return await operation(_netTcpClient);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "NetTcp failed for {Operation}, trying HTTP fallback", operationName);
                // Fallback to HTTP
                return await operation(_httpClient);
            }
        }
        
        // Default: Try HTTP first, fallback to NetTcp
        try
        {
            _logger.LogDebug("Using HTTP client for {Operation}", operationName);
            return await operation(_httpClient);
        }
        catch (Exception ex) when (ex is AxException || ex is HttpRequestException || ex is TimeoutException)
        {
            _logger.LogWarning(ex, "HTTP failed for {Operation}, trying NetTcp fallback", operationName);
            
            // If HTTP fails, try NetTcp
            try
            {
                _logger.LogInformation("Switching to NetTcp for {Operation} due to HTTP failure", operationName);
                _preferNetTcp = true; // Remember preference for future calls
                return await operation(_netTcpClient);
            }
            catch (Exception netTcpEx)
            {
                _logger.LogError(netTcpEx, "Both HTTP and NetTcp failed for {Operation}", operationName);
                throw;
            }
        }
    }
    
    public async Task<Customer?> GetCustomerAsync(string customerAccount, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithFallbackAsync(
            client => client.GetCustomerAsync(customerAccount, cancellationToken),
            "GetCustomer");
    }
    
    public async Task<IEnumerable<Customer>> SearchCustomersAsync(string searchTerm, int maxResults = 5, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithFallbackAsync(
            client => client.SearchCustomersAsync(searchTerm, maxResults, cancellationToken),
            "SearchCustomers");
    }
    
    public async Task<Item?> GetItemAsync(string itemId, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithFallbackAsync(
            client => client.GetItemAsync(itemId, cancellationToken),
            "GetItem");
    }
    
    public async Task<SalesOrder?> GetSalesOrderAsync(string salesId, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithFallbackAsync(
            client => client.GetSalesOrderAsync(salesId, cancellationToken),
            "GetSalesOrder");
    }
    
    public async Task<IEnumerable<SalesOrder>> GetSalesOrdersByCustomerAsync(
        string customerAccount, 
        SalesOrderFilter? filter = null, 
        CancellationToken cancellationToken = default)
    {
        return await ExecuteWithFallbackAsync(
            client => client.GetSalesOrdersByCustomerAsync(customerAccount, filter, cancellationToken),
            "GetSalesOrdersByCustomer");
    }
    
    public async Task<InventoryOnHand> GetInventoryOnHandAsync(string itemId, string? warehouseId = null, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithFallbackAsync(
            client => client.GetInventoryOnHandAsync(itemId, warehouseId, cancellationToken),
            "GetInventoryOnHand");
    }
    
    public async Task<PriceResult> SimulatePriceAsync(
        string customerAccount, 
        string itemId, 
        decimal quantity, 
        DateTime? date = null, 
        CancellationToken cancellationToken = default)
    {
        return await ExecuteWithFallbackAsync(
            client => client.SimulatePriceAsync(customerAccount, itemId, quantity, date, cancellationToken),
            "SimulatePrice");
    }
    
    public async Task<IEnumerable<ReservationQueueEntry>> GetReservationQueueAsync(
        string itemId, 
        string? warehouseId = null, 
        CancellationToken cancellationToken = default)
    {
        return await ExecuteWithFallbackAsync(
            client => client.GetReservationQueueAsync(itemId, warehouseId, cancellationToken),
            "GetReservationQueue");
    }
}

