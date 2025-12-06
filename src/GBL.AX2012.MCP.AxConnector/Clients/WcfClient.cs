using System.Net;
using System.ServiceModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using GBL.AX2012.MCP.Core.Exceptions;
using GBL.AX2012.MCP.Core.Interfaces;
using GBL.AX2012.MCP.Core.Options;
using GBL.AX2012.MCP.AxConnector.Interfaces;

namespace GBL.AX2012.MCP.AxConnector.Clients;

public class WcfClient : IWcfClient, IDisposable
{
    private readonly ILogger<WcfClient> _logger;
    private readonly ICircuitBreaker _circuitBreaker;
    private readonly WcfClientOptions _options;
    private readonly ChannelFactory<IGblSalesOrderService>? _channelFactory;
    
    public WcfClient(
        IOptions<WcfClientOptions> options,
        ILogger<WcfClient> logger,
        ICircuitBreaker circuitBreaker)
    {
        _options = options.Value;
        _logger = logger;
        _circuitBreaker = circuitBreaker;
        
        try
        {
            var binding = new BasicHttpBinding
            {
                Security = new BasicHttpSecurity
                {
                    Mode = BasicHttpSecurityMode.TransportCredentialOnly,
                    Transport = new HttpTransportSecurity
                    {
                        ClientCredentialType = HttpClientCredentialType.Windows
                    }
                },
                MaxReceivedMessageSize = 10 * 1024 * 1024,
                SendTimeout = _options.Timeout,
                ReceiveTimeout = _options.Timeout
            };
            
            var endpoint = new EndpointAddress(_options.BaseUrl);
            _channelFactory = new ChannelFactory<IGblSalesOrderService>(binding, endpoint);
            
            if (!string.IsNullOrEmpty(_options.ServiceAccountUser))
            {
                _channelFactory.Credentials.Windows.ClientCredential = new NetworkCredential(
                    _options.ServiceAccountUser,
                    _options.ServiceAccountPassword,
                    _options.ServiceAccountDomain);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to initialize WCF channel factory - WCF operations will fail");
        }
    }
    
    public async Task<string> CreateSalesOrderAsync(CreateSalesOrderRequest request, CancellationToken cancellationToken = default)
    {
        return await _circuitBreaker.ExecuteAsync(async () =>
        {
            if (_channelFactory == null)
            {
                throw new AxException("WCF_NOT_CONFIGURED", "WCF client is not properly configured");
            }
            
            var channel = _channelFactory.CreateChannel();
            
            try
            {
                _logger.LogDebug("Creating sales order for customer {Customer}", request.CustomerAccount);
                
                var wcfRequest = new WcfCreateSalesOrderRequest
                {
                    CustomerAccount = request.CustomerAccount,
                    RequestedDeliveryDate = request.RequestedDeliveryDate,
                    CustomerRef = request.CustomerRef,
                    Lines = request.Lines.Select(l => new WcfSalesLineRequest
                    {
                        ItemId = l.ItemId,
                        Quantity = l.Quantity,
                        UnitPrice = l.UnitPrice ?? 0,
                        WarehouseId = l.WarehouseId ?? "WH-MAIN"
                    }).ToArray()
                };
                
                var response = await channel.CreateSalesOrderAsync(wcfRequest);
                
                if (!response.Success)
                {
                    _logger.LogError("AX returned error: {Code} - {Message}", response.ErrorCode, response.ErrorMessage);
                    throw new AxException(response.ErrorCode ?? "AX_ERROR", response.ErrorMessage ?? "Unknown error");
                }
                
                _logger.LogInformation("Created sales order {SalesId}", response.SalesId);
                return response.SalesId!;
            }
            finally
            {
                CloseChannel(channel);
            }
        }, cancellationToken);
    }
    
    public async Task<bool> UpdateSalesOrderAsync(UpdateSalesOrderRequest request, CancellationToken cancellationToken = default)
    {
        return await _circuitBreaker.ExecuteAsync(async () =>
        {
            if (_channelFactory == null)
            {
                throw new AxException("WCF_NOT_CONFIGURED", "WCF client is not properly configured");
            }
            
            var channel = _channelFactory.CreateChannel();
            
            try
            {
                var wcfRequest = new WcfUpdateSalesOrderRequest
                {
                    SalesId = request.SalesId,
                    RequestedDeliveryDate = request.RequestedDeliveryDate,
                    CustomerRef = request.CustomerRef
                };
                
                var response = await channel.UpdateSalesOrderAsync(wcfRequest);
                return response.Success;
            }
            finally
            {
                CloseChannel(channel);
            }
        }, cancellationToken);
    }
    
    public async Task<int> AddSalesLineAsync(SalesLineCreateRequest request, CancellationToken cancellationToken = default)
    {
        return await _circuitBreaker.ExecuteAsync(async () =>
        {
            if (_channelFactory == null)
            {
                throw new AxException("WCF_NOT_CONFIGURED", "WCF client is not properly configured");
            }
            
            var channel = _channelFactory.CreateChannel();
            
            try
            {
                _logger.LogDebug("Adding sales line to order {SalesId}: {ItemId} x {Quantity}", 
                    request.SalesId, request.ItemId, request.Quantity);
                
                var wcfRequest = new WcfAddSalesLineRequest
                {
                    SalesId = request.SalesId,
                    ItemId = request.ItemId,
                    Quantity = request.Quantity,
                    UnitId = request.UnitId ?? "pcs",
                    UnitPrice = request.UnitPrice,
                    DiscountPercent = request.DiscountPercent,
                    WarehouseId = request.WarehouseId ?? "WH-MAIN",
                    RequestedDeliveryDate = request.RequestedDeliveryDate
                };
                
                var response = await channel.AddSalesLineAsync(wcfRequest);
                
                if (!response.Success)
                {
                    _logger.LogError("Failed to add sales line: {Code} - {Message}", response.ErrorCode, response.ErrorMessage);
                    throw new AxException(response.ErrorCode ?? "AX_ERROR", response.ErrorMessage ?? "Unknown error");
                }
                
                _logger.LogInformation("Added line {LineNum} to order {SalesId}", response.LineNum, request.SalesId);
                return response.LineNum;
            }
            finally
            {
                CloseChannel(channel);
            }
        }, cancellationToken);
    }
    
    private void CloseChannel(IGblSalesOrderService channel)
    {
        try
        {
            ((IClientChannel)channel).Close();
        }
        catch
        {
            ((IClientChannel)channel).Abort();
        }
    }
    
    public void Dispose()
    {
        try
        {
            _channelFactory?.Close();
        }
        catch
        {
            _channelFactory?.Abort();
        }
    }
}

// WCF Service Contract
[ServiceContract(Namespace = "http://gbl.com/ax2012/services")]
public interface IGblSalesOrderService
{
    [OperationContract]
    Task<WcfCreateSalesOrderResponse> CreateSalesOrderAsync(WcfCreateSalesOrderRequest request);
    
    [OperationContract]
    Task<WcfUpdateSalesOrderResponse> UpdateSalesOrderAsync(WcfUpdateSalesOrderRequest request);
    
    [OperationContract]
    Task<WcfAddSalesLineResponse> AddSalesLineAsync(WcfAddSalesLineRequest request);
}

// WCF Data Contracts
[System.Runtime.Serialization.DataContract(Namespace = "http://gbl.com/ax2012/services")]
public class WcfCreateSalesOrderRequest
{
    [System.Runtime.Serialization.DataMember] public string CustomerAccount { get; set; } = "";
    [System.Runtime.Serialization.DataMember] public DateTime RequestedDeliveryDate { get; set; }
    [System.Runtime.Serialization.DataMember] public string? CustomerRef { get; set; }
    [System.Runtime.Serialization.DataMember] public WcfSalesLineRequest[] Lines { get; set; } = [];
}

[System.Runtime.Serialization.DataContract(Namespace = "http://gbl.com/ax2012/services")]
public class WcfSalesLineRequest
{
    [System.Runtime.Serialization.DataMember] public string ItemId { get; set; } = "";
    [System.Runtime.Serialization.DataMember] public decimal Quantity { get; set; }
    [System.Runtime.Serialization.DataMember] public decimal UnitPrice { get; set; }
    [System.Runtime.Serialization.DataMember] public string WarehouseId { get; set; } = "";
}

[System.Runtime.Serialization.DataContract(Namespace = "http://gbl.com/ax2012/services")]
public class WcfCreateSalesOrderResponse
{
    [System.Runtime.Serialization.DataMember] public bool Success { get; set; }
    [System.Runtime.Serialization.DataMember] public string? SalesId { get; set; }
    [System.Runtime.Serialization.DataMember] public string? ErrorCode { get; set; }
    [System.Runtime.Serialization.DataMember] public string? ErrorMessage { get; set; }
}

[System.Runtime.Serialization.DataContract(Namespace = "http://gbl.com/ax2012/services")]
public class WcfUpdateSalesOrderRequest
{
    [System.Runtime.Serialization.DataMember] public string SalesId { get; set; } = "";
    [System.Runtime.Serialization.DataMember] public DateTime? RequestedDeliveryDate { get; set; }
    [System.Runtime.Serialization.DataMember] public string? CustomerRef { get; set; }
}

[System.Runtime.Serialization.DataContract(Namespace = "http://gbl.com/ax2012/services")]
public class WcfUpdateSalesOrderResponse
{
    [System.Runtime.Serialization.DataMember] public bool Success { get; set; }
    [System.Runtime.Serialization.DataMember] public string? ErrorCode { get; set; }
    [System.Runtime.Serialization.DataMember] public string? ErrorMessage { get; set; }
}

[System.Runtime.Serialization.DataContract(Namespace = "http://gbl.com/ax2012/services")]
public class WcfAddSalesLineRequest
{
    [System.Runtime.Serialization.DataMember] public string SalesId { get; set; } = "";
    [System.Runtime.Serialization.DataMember] public string ItemId { get; set; } = "";
    [System.Runtime.Serialization.DataMember] public decimal Quantity { get; set; }
    [System.Runtime.Serialization.DataMember] public string UnitId { get; set; } = "";
    [System.Runtime.Serialization.DataMember] public decimal UnitPrice { get; set; }
    [System.Runtime.Serialization.DataMember] public decimal DiscountPercent { get; set; }
    [System.Runtime.Serialization.DataMember] public string WarehouseId { get; set; } = "";
    [System.Runtime.Serialization.DataMember] public DateTime? RequestedDeliveryDate { get; set; }
}

[System.Runtime.Serialization.DataContract(Namespace = "http://gbl.com/ax2012/services")]
public class WcfAddSalesLineResponse
{
    [System.Runtime.Serialization.DataMember] public bool Success { get; set; }
    [System.Runtime.Serialization.DataMember] public int LineNum { get; set; }
    [System.Runtime.Serialization.DataMember] public string? ErrorCode { get; set; }
    [System.Runtime.Serialization.DataMember] public string? ErrorMessage { get; set; }
}
