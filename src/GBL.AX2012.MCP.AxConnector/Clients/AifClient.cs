using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using GBL.AX2012.MCP.Core.Exceptions;
using GBL.AX2012.MCP.Core.Interfaces;
using GBL.AX2012.MCP.Core.Models;
using GBL.AX2012.MCP.Core.Options;
using GBL.AX2012.MCP.AxConnector.Helpers;
using GBL.AX2012.MCP.AxConnector.Interfaces;

namespace GBL.AX2012.MCP.AxConnector.Clients;

public class AifClient : IAifClient
{
    private readonly HttpClient _httpClient;
    private readonly AifClientOptions _options;
    private readonly ILogger<AifClient> _logger;
    private readonly ICircuitBreaker _circuitBreaker;
    
    private const string SoapNamespace = "http://schemas.xmlsoap.org/soap/envelope/";
    private const string AxNamespace = "http://schemas.microsoft.com/dynamics/2008/01/services";
    
    public AifClient(
        HttpClient httpClient,
        IOptions<AifClientOptions> options,
        ILogger<AifClient> logger,
        ICircuitBreaker circuitBreaker)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        _circuitBreaker = circuitBreaker;
    }
    
    public async Task<Customer?> GetCustomerAsync(string customerAccount, CancellationToken cancellationToken = default)
    {
        return await _circuitBreaker.ExecuteAsync(async () =>
        {
            var soapRequest = BuildFindRequest("CustCustomerService", $@"
                <CustTable class=""entity"">
                    <AccountNum>{customerAccount}</AccountNum>
                </CustTable>");
            
            var response = await SendSoapRequestAsync("CustCustomerService", soapRequest, cancellationToken);
            return ParseCustomerResponse(response);
        }, cancellationToken);
    }
    
    public async Task<IEnumerable<Customer>> SearchCustomersAsync(string searchTerm, int maxResults = 5, CancellationToken cancellationToken = default)
    {
        return await _circuitBreaker.ExecuteAsync(async () =>
        {
            var soapRequest = BuildFindRequest("CustCustomerService", $@"
                <CustTable class=""entity"">
                    <Name>*{searchTerm}*</Name>
                </CustTable>");
            
            var response = await SendSoapRequestAsync("CustCustomerService", soapRequest, cancellationToken);
            var customers = ParseCustomersResponse(response);
            
            return customers
                .Select(c => c with { MatchConfidence = FuzzyMatch.Score(searchTerm, c.Name) })
                .OrderByDescending(c => c.MatchConfidence)
                .Take(maxResults);
        }, cancellationToken);
    }
    
    public async Task<Item?> GetItemAsync(string itemId, CancellationToken cancellationToken = default)
    {
        return await _circuitBreaker.ExecuteAsync(async () =>
        {
            var soapRequest = BuildFindRequest("EcoResProductService", $@"
                <InventTable class=""entity"">
                    <ItemId>{itemId}</ItemId>
                </InventTable>");
            
            var response = await SendSoapRequestAsync("EcoResProductService", soapRequest, cancellationToken);
            return ParseItemResponse(response);
        }, cancellationToken);
    }
    
    public async Task<SalesOrder?> GetSalesOrderAsync(string salesId, CancellationToken cancellationToken = default)
    {
        return await _circuitBreaker.ExecuteAsync(async () =>
        {
            var soapRequest = BuildReadRequest("SalesSalesOrderService", salesId);
            var response = await SendSoapRequestAsync("SalesSalesOrderService", soapRequest, cancellationToken);
            return ParseSalesOrderResponse(response);
        }, cancellationToken);
    }
    
    public async Task<IEnumerable<SalesOrder>> GetSalesOrdersByCustomerAsync(
        string customerAccount, 
        SalesOrderFilter? filter = null, 
        CancellationToken cancellationToken = default)
    {
        return await _circuitBreaker.ExecuteAsync(async () =>
        {
            var criteria = $"<CustAccount>{customerAccount}</CustAccount>";
            
            if (filter?.StatusFilter?.Any() == true)
            {
                criteria += $"<SalesStatus>{string.Join(",", filter.StatusFilter)}</SalesStatus>";
            }
            
            var soapRequest = BuildFindRequest("SalesSalesOrderService", $@"
                <SalesTable class=""entity"">
                    {criteria}
                </SalesTable>");
            
            var response = await SendSoapRequestAsync("SalesSalesOrderService", soapRequest, cancellationToken);
            var orders = ParseSalesOrdersResponse(response);
            
            return orders
                .OrderByDescending(o => o.OrderDate)
                .Skip(filter?.Skip ?? 0)
                .Take(filter?.Take ?? 20);
        }, cancellationToken);
    }
    
    public async Task<InventoryOnHand> GetInventoryOnHandAsync(string itemId, string? warehouseId = null, CancellationToken cancellationToken = default)
    {
        return await _circuitBreaker.ExecuteAsync(async () =>
        {
            var criteria = $"<ItemId>{itemId}</ItemId>";
            if (!string.IsNullOrEmpty(warehouseId))
            {
                criteria += $"<InventLocationId>{warehouseId}</InventLocationId>";
            }
            
            var soapRequest = BuildFindRequest("InventInventSumService", $@"
                <InventSum class=""entity"">
                    {criteria}
                </InventSum>");
            
            var response = await SendSoapRequestAsync("InventInventSumService", soapRequest, cancellationToken);
            return ParseInventoryResponse(response, itemId);
        }, cancellationToken);
    }
    
    public async Task<PriceResult> SimulatePriceAsync(
        string customerAccount, 
        string itemId, 
        decimal quantity, 
        DateTime? date = null, 
        CancellationToken cancellationToken = default)
    {
        return await _circuitBreaker.ExecuteAsync(async () =>
        {
            // Use custom GBL service for price simulation
            var priceDate = date ?? DateTime.Today;
            var soapRequest = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""{SoapNamespace}"" xmlns:gbl=""http://gbl.com/ax2012/services"">
    <soap:Body>
        <gbl:simulatePrice>
            <gbl:customerAccount>{customerAccount}</gbl:customerAccount>
            <gbl:itemId>{itemId}</gbl:itemId>
            <gbl:quantity>{quantity}</gbl:quantity>
            <gbl:priceDate>{priceDate:yyyy-MM-dd}</gbl:priceDate>
        </gbl:simulatePrice>
    </soap:Body>
</soap:Envelope>";
            
            var response = await SendSoapRequestAsync("GBL_PriceSimulationService", soapRequest, cancellationToken);
            return ParsePriceResponse(response);
        }, cancellationToken);
    }
    
    public async Task<IEnumerable<ReservationQueueEntry>> GetReservationQueueAsync(
        string itemId, 
        string? warehouseId = null, 
        CancellationToken cancellationToken = default)
    {
        return await _circuitBreaker.ExecuteAsync(async () =>
        {
            var warehouseCriteria = warehouseId != null 
                ? $"<InventDimId>{warehouseId}</InventDimId>" 
                : "";
            
            var soapRequest = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""{SoapNamespace}"" xmlns:gbl=""http://gbl.com/ax2012/services"">
    <soap:Body>
        <gbl:getReservationQueue>
            <gbl:itemId>{itemId}</gbl:itemId>
            {warehouseCriteria}
        </gbl:getReservationQueue>
    </soap:Body>
</soap:Envelope>";
            
            var response = await SendSoapRequestAsync("GBL_InventoryService", soapRequest, cancellationToken);
            return ParseReservationQueueResponse(response);
        }, cancellationToken);
    }
    
    private IEnumerable<ReservationQueueEntry> ParseReservationQueueResponse(string response)
    {
        var entries = new List<ReservationQueueEntry>();
        
        try
        {
            var doc = XDocument.Parse(response);
            var ns = XNamespace.Get("http://gbl.com/ax2012/services");
            
            var queueEntries = doc.Descendants(ns + "ReservationQueueEntry");
            
            foreach (var entry in queueEntries)
            {
                entries.Add(new ReservationQueueEntry
                {
                    SalesId = entry.Element(ns + "SalesId")?.Value ?? "",
                    LineNum = int.TryParse(entry.Element(ns + "LineNum")?.Value, out var ln) ? ln : 0,
                    CustomerAccount = entry.Element(ns + "CustomerAccount")?.Value ?? "",
                    CustomerName = entry.Element(ns + "CustomerName")?.Value ?? "",
                    ItemId = entry.Element(ns + "ItemId")?.Value ?? "",
                    ReservedQty = decimal.TryParse(entry.Element(ns + "ReservedQty")?.Value, out var rq) ? rq : 0,
                    PendingQty = decimal.TryParse(entry.Element(ns + "PendingQty")?.Value, out var pq) ? pq : 0,
                    RequestedDate = DateTime.TryParse(entry.Element(ns + "RequestedDate")?.Value, out var rd) ? rd : DateTime.MinValue,
                    OrderDate = DateTime.TryParse(entry.Element(ns + "OrderDate")?.Value, out var od) ? od : DateTime.MinValue,
                    Priority = int.TryParse(entry.Element(ns + "Priority")?.Value, out var p) ? p : 0
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse reservation queue response");
        }
        
        return entries.OrderBy(e => e.Priority).ThenBy(e => e.OrderDate);
    }
    
    private string BuildFindRequest(string service, string criteria)
    {
        return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""{SoapNamespace}"" xmlns:ax=""{AxNamespace}"">
    <soap:Body>
        <ax:find>
            <ax:QueryCriteria>
                {criteria}
            </ax:QueryCriteria>
        </ax:find>
    </soap:Body>
</soap:Envelope>";
    }
    
    private string BuildReadRequest(string service, string key)
    {
        return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""{SoapNamespace}"" xmlns:ax=""{AxNamespace}"">
    <soap:Body>
        <ax:read>
            <ax:_entityKeyList>
                <ax:KeyData>
                    <ax:KeyField>
                        <ax:Field>SalesId</ax:Field>
                        <ax:Value>{key}</ax:Value>
                    </ax:KeyField>
                </ax:KeyData>
            </ax:_entityKeyList>
        </ax:read>
    </soap:Body>
</soap:Envelope>";
    }
    
    private async Task<string> SendSoapRequestAsync(string service, string soapRequest, CancellationToken cancellationToken)
    {
        var url = $"{_options.BaseUrl}/{service}";
        
        _logger.LogDebug("Sending SOAP request to {Url}", url);
        
        var content = new StringContent(soapRequest, Encoding.UTF8, "text/xml");
        content.Headers.Add("SOAPAction", "");
        
        var response = await _httpClient.PostAsync(url, content, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("AIF request failed: {Status} - {Body}", response.StatusCode, errorBody);
            throw new AxException("AIF_ERROR", $"AIF request failed: {response.StatusCode}");
        }
        
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
    
    private Customer? ParseCustomerResponse(string response)
    {
        try
        {
            var doc = XDocument.Parse(response);
            XNamespace ns = AxNamespace;
            
            var custTable = doc.Descendants(ns + "CustTable").FirstOrDefault();
            if (custTable == null) return null;
            
            return new Customer
            {
                AccountNum = custTable.Element(ns + "AccountNum")?.Value ?? "",
                Name = custTable.Element(ns + "Name")?.Value ?? "",
                Currency = custTable.Element(ns + "Currency")?.Value ?? "EUR",
                CreditLimit = decimal.TryParse(custTable.Element(ns + "CreditMax")?.Value, out var cl) ? cl : 0,
                CreditUsed = decimal.TryParse(custTable.Element(ns + "BalanceMST")?.Value, out var cu) ? cu : 0,
                PaymentTerms = custTable.Element(ns + "PaymTermId")?.Value ?? "",
                PriceGroup = custTable.Element(ns + "PriceGroup")?.Value ?? "",
                Blocked = custTable.Element(ns + "Blocked")?.Value == "1"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse customer response");
            return null;
        }
    }
    
    private IEnumerable<Customer> ParseCustomersResponse(string response)
    {
        var customers = new List<Customer>();
        try
        {
            var doc = XDocument.Parse(response);
            XNamespace ns = AxNamespace;
            
            foreach (var custTable in doc.Descendants(ns + "CustTable"))
            {
                customers.Add(new Customer
                {
                    AccountNum = custTable.Element(ns + "AccountNum")?.Value ?? "",
                    Name = custTable.Element(ns + "Name")?.Value ?? "",
                    Currency = custTable.Element(ns + "Currency")?.Value ?? "EUR",
                    CreditLimit = decimal.TryParse(custTable.Element(ns + "CreditMax")?.Value, out var cl) ? cl : 0,
                    CreditUsed = decimal.TryParse(custTable.Element(ns + "BalanceMST")?.Value, out var cu) ? cu : 0,
                    PaymentTerms = custTable.Element(ns + "PaymTermId")?.Value ?? "",
                    PriceGroup = custTable.Element(ns + "PriceGroup")?.Value ?? "",
                    Blocked = custTable.Element(ns + "Blocked")?.Value == "1"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse customers response");
        }
        return customers;
    }
    
    private Item? ParseItemResponse(string response)
    {
        try
        {
            var doc = XDocument.Parse(response);
            XNamespace ns = AxNamespace;
            
            var inventTable = doc.Descendants(ns + "InventTable").FirstOrDefault();
            if (inventTable == null) return null;
            
            return new Item
            {
                ItemId = inventTable.Element(ns + "ItemId")?.Value ?? "",
                Name = inventTable.Element(ns + "Name")?.Value ?? "",
                ItemGroup = inventTable.Element(ns + "ItemGroupId")?.Value ?? "",
                Unit = inventTable.Element(ns + "UnitId")?.Value ?? "PCS",
                BlockedForSales = inventTable.Element(ns + "BlockedForSales")?.Value == "1"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse item response");
            return null;
        }
    }
    
    private SalesOrder? ParseSalesOrderResponse(string response)
    {
        try
        {
            var doc = XDocument.Parse(response);
            XNamespace ns = AxNamespace;
            
            var salesTable = doc.Descendants(ns + "SalesTable").FirstOrDefault();
            if (salesTable == null) return null;
            
            var order = new SalesOrder
            {
                SalesId = salesTable.Element(ns + "SalesId")?.Value ?? "",
                CustomerAccount = salesTable.Element(ns + "CustAccount")?.Value ?? "",
                CustomerName = salesTable.Element(ns + "SalesName")?.Value ?? "",
                OrderDate = DateTime.TryParse(salesTable.Element(ns + "CreatedDateTime")?.Value, out var od) ? od : DateTime.Today,
                RequestedDelivery = DateTime.TryParse(salesTable.Element(ns + "DeliveryDate")?.Value, out var dd) ? dd : DateTime.Today,
                Status = MapSalesStatus(salesTable.Element(ns + "SalesStatus")?.Value),
                TotalAmount = decimal.TryParse(salesTable.Element(ns + "SalesBalance")?.Value, out var ta) ? ta : 0,
                Currency = salesTable.Element(ns + "CurrencyCode")?.Value ?? "EUR",
                Lines = salesTable.Descendants(ns + "SalesLine")
                    .Select(line => new SalesLine
                    {
                        LineNum = int.TryParse(line.Element(ns + "LineNum")?.Value, out var ln) ? ln : 0,
                        ItemId = line.Element(ns + "ItemId")?.Value ?? "",
                        ItemName = line.Element(ns + "Name")?.Value ?? "",
                        Quantity = decimal.TryParse(line.Element(ns + "SalesQty")?.Value, out var q) ? q : 0,
                        UnitPrice = decimal.TryParse(line.Element(ns + "SalesPrice")?.Value, out var p) ? p : 0,
                        LineAmount = decimal.TryParse(line.Element(ns + "LineAmount")?.Value, out var la) ? la : 0,
                        ReservedQty = decimal.TryParse(line.Element(ns + "ReservedPhysical")?.Value, out var rq) ? rq : 0,
                        DeliveredQty = decimal.TryParse(line.Element(ns + "DeliveredQty")?.Value, out var dq) ? dq : 0
                    })
                    .ToList()
            };
            
            return order;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse sales order response");
            return null;
        }
    }
    
    private IEnumerable<SalesOrder> ParseSalesOrdersResponse(string response)
    {
        var orders = new List<SalesOrder>();
        try
        {
            var doc = XDocument.Parse(response);
            XNamespace ns = AxNamespace;
            
            foreach (var salesTable in doc.Descendants(ns + "SalesTable"))
            {
                orders.Add(new SalesOrder
                {
                    SalesId = salesTable.Element(ns + "SalesId")?.Value ?? "",
                    CustomerAccount = salesTable.Element(ns + "CustAccount")?.Value ?? "",
                    CustomerName = salesTable.Element(ns + "SalesName")?.Value ?? "",
                    OrderDate = DateTime.TryParse(salesTable.Element(ns + "CreatedDateTime")?.Value, out var od) ? od : DateTime.Today,
                    RequestedDelivery = DateTime.TryParse(salesTable.Element(ns + "DeliveryDate")?.Value, out var dd) ? dd : DateTime.Today,
                    Status = MapSalesStatus(salesTable.Element(ns + "SalesStatus")?.Value),
                    TotalAmount = decimal.TryParse(salesTable.Element(ns + "SalesBalance")?.Value, out var ta) ? ta : 0,
                    Currency = salesTable.Element(ns + "CurrencyCode")?.Value ?? "EUR"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse sales orders response");
        }
        return orders;
    }
    
    private InventoryOnHand ParseInventoryResponse(string response, string itemId)
    {
        var result = new InventoryOnHand { ItemId = itemId };
        var warehouses = new List<WarehouseInventory>();
        
        try
        {
            var doc = XDocument.Parse(response);
            XNamespace ns = AxNamespace;
            
            foreach (var inventSum in doc.Descendants(ns + "InventSum"))
            {
                var wh = new WarehouseInventory
                {
                    WarehouseId = inventSum.Element(ns + "InventLocationId")?.Value ?? "",
                    OnHand = decimal.TryParse(inventSum.Element(ns + "PhysicalInvent")?.Value, out var oh) ? oh : 0,
                    Available = decimal.TryParse(inventSum.Element(ns + "AvailPhysical")?.Value, out var av) ? av : 0,
                    Reserved = decimal.TryParse(inventSum.Element(ns + "ReservPhysical")?.Value, out var rs) ? rs : 0
                };
                warehouses.Add(wh);
            }
            
            result = result with
            {
                TotalOnHand = warehouses.Sum(w => w.OnHand),
                Available = warehouses.Sum(w => w.Available),
                Reserved = warehouses.Sum(w => w.Reserved),
                Warehouses = warehouses
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse inventory response");
        }
        
        return result;
    }
    
    private PriceResult ParsePriceResponse(string response)
    {
        try
        {
            var doc = XDocument.Parse(response);
            XNamespace gbl = "http://gbl.com/ax2012/services";
            
            var result = doc.Descendants(gbl + "simulatePriceResult").FirstOrDefault();
            if (result == null) return new PriceResult();
            
            return new PriceResult
            {
                BasePrice = decimal.TryParse(result.Element(gbl + "basePrice")?.Value, out var bp) ? bp : 0,
                CustomerDiscountPct = decimal.TryParse(result.Element(gbl + "customerDiscountPct")?.Value, out var cd) ? cd : 0,
                QuantityDiscountPct = decimal.TryParse(result.Element(gbl + "quantityDiscountPct")?.Value, out var qd) ? qd : 0,
                FinalUnitPrice = decimal.TryParse(result.Element(gbl + "finalUnitPrice")?.Value, out var fp) ? fp : 0,
                LineAmount = decimal.TryParse(result.Element(gbl + "lineAmount")?.Value, out var la) ? la : 0,
                Currency = result.Element(gbl + "currency")?.Value ?? "EUR",
                PriceSource = result.Element(gbl + "priceSource")?.Value ?? "",
                ValidUntil = DateTime.TryParse(result.Element(gbl + "validUntil")?.Value, out var vu) ? vu : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse price response");
            return new PriceResult();
        }
    }
    
    private static string MapSalesStatus(string? axStatus) => axStatus switch
    {
        "1" => "Open",
        "2" => "Delivered",
        "3" => "Invoiced",
        "4" => "Cancelled",
        _ => "Unknown"
    };
}
