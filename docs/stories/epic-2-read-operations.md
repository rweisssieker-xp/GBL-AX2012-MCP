---
epic: 2
title: "Read Operations (Customer & Orders)"
stories: 6
status: "READY"
project_name: "GBL-AX2012-MCP"
date: "2025-12-06"
---

# Epic 2: Read Operations - Implementation Plans

## Story 2.1: AIF Client Setup

### Implementation Plan

```
📁 AIF Client
│
├── 1. Create IAifClient interface
│   └── Define all read operations
│
├── 2. Create AifClient implementation
│   └── SOAP/HTTP client with Windows Auth
│
├── 3. Create SOAP helpers
│   └── Envelope builder, response parser
│
├── 4. Add HttpClient configuration
│   └── Windows Auth handler
│
└── 5. Integration test
    └── Test against AX test environment
```

### Files to Create

```csharp
// src/GBL.AX2012.MCP.AxConnector/Interfaces/IAifClient.cs
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
}

// src/GBL.AX2012.MCP.AxConnector/Clients/AifClient.cs
namespace GBL.AX2012.MCP.AxConnector.Clients;

public class AifClient : IAifClient
{
    private readonly HttpClient _httpClient;
    private readonly AifClientOptions _options;
    private readonly ILogger<AifClient> _logger;
    private readonly ICircuitBreaker _circuitBreaker;
    
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
            var soapRequest = SoapHelper.BuildFindRequest("CustCustomerService", new
            {
                CustTable = new { AccountNum = customerAccount }
            });
            
            var response = await SendSoapRequestAsync("CustCustomerService", soapRequest, cancellationToken);
            return SoapHelper.ParseCustomerResponse(response);
        }, cancellationToken);
    }
    
    public async Task<IEnumerable<Customer>> SearchCustomersAsync(string searchTerm, int maxResults = 5, CancellationToken cancellationToken = default)
    {
        return await _circuitBreaker.ExecuteAsync(async () =>
        {
            var soapRequest = SoapHelper.BuildFindRequest("CustCustomerService", new
            {
                CustTable = new { Name = $"*{searchTerm}*" }
            });
            
            var response = await SendSoapRequestAsync("CustCustomerService", soapRequest, cancellationToken);
            var customers = SoapHelper.ParseCustomersResponse(response);
            
            // Apply fuzzy matching and scoring
            return customers
                .Select(c => new { Customer = c, Score = FuzzyMatch.Score(searchTerm, c.Name) })
                .OrderByDescending(x => x.Score)
                .Take(maxResults)
                .Select(x => x.Customer with { MatchConfidence = x.Score });
        }, cancellationToken);
    }
    
    public async Task<SalesOrder?> GetSalesOrderAsync(string salesId, CancellationToken cancellationToken = default)
    {
        return await _circuitBreaker.ExecuteAsync(async () =>
        {
            var soapRequest = SoapHelper.BuildReadRequest("SalesSalesOrderService", new[]
            {
                new { Field = "SalesId", Value = salesId }
            });
            
            var response = await SendSoapRequestAsync("SalesSalesOrderService", soapRequest, cancellationToken);
            return SoapHelper.ParseSalesOrderResponse(response);
        }, cancellationToken);
    }
    
    public async Task<IEnumerable<SalesOrder>> GetSalesOrdersByCustomerAsync(
        string customerAccount, 
        SalesOrderFilter? filter = null, 
        CancellationToken cancellationToken = default)
    {
        return await _circuitBreaker.ExecuteAsync(async () =>
        {
            var criteria = new Dictionary<string, object>
            {
                ["CustAccount"] = customerAccount
            };
            
            if (filter?.StatusFilter?.Any() == true)
            {
                criteria["SalesStatus"] = filter.StatusFilter;
            }
            
            if (filter?.DateFrom.HasValue == true)
            {
                criteria["CreatedDateTime"] = new { From = filter.DateFrom.Value };
            }
            
            var soapRequest = SoapHelper.BuildFindRequest("SalesSalesOrderService", new { SalesTable = criteria });
            var response = await SendSoapRequestAsync("SalesSalesOrderService", soapRequest, cancellationToken);
            
            var orders = SoapHelper.ParseSalesOrdersResponse(response);
            
            // Apply pagination
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
            var criteria = new Dictionary<string, object> { ["ItemId"] = itemId };
            if (warehouseId != null)
            {
                criteria["InventLocationId"] = warehouseId;
            }
            
            var soapRequest = SoapHelper.BuildFindRequest("InventInventSumService", new { InventSum = criteria });
            var response = await SendSoapRequestAsync("InventInventSumService", soapRequest, cancellationToken);
            
            return SoapHelper.ParseInventoryResponse(response, itemId);
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
            // Use custom service for price simulation
            var soapRequest = SoapHelper.BuildCustomRequest("GBL_PriceSimulationService", "simulatePrice", new
            {
                customerAccount,
                itemId,
                quantity,
                priceDate = date ?? DateTime.Today
            });
            
            var response = await SendSoapRequestAsync("GBL_PriceSimulationService", soapRequest, cancellationToken);
            return SoapHelper.ParsePriceResponse(response);
        }, cancellationToken);
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
}

// src/GBL.AX2012.MCP.AxConnector/Helpers/SoapHelper.cs
namespace GBL.AX2012.MCP.AxConnector.Helpers;

public static class SoapHelper
{
    private const string SoapNamespace = "http://schemas.xmlsoap.org/soap/envelope/";
    private const string AxNamespace = "http://schemas.microsoft.com/dynamics/2008/01/services";
    
    public static string BuildFindRequest(string service, object criteria)
    {
        var criteriaXml = SerializeCriteria(criteria);
        
        return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""{SoapNamespace}"" xmlns:ax=""{AxNamespace}"">
    <soap:Body>
        <ax:find>
            <ax:_criteria>{criteriaXml}</ax:_criteria>
        </ax:find>
    </soap:Body>
</soap:Envelope>";
    }
    
    public static string BuildReadRequest(string service, object[] keys)
    {
        var keysXml = string.Join("", keys.Select(k => $"<ax:KeyData>{SerializeKey(k)}</ax:KeyData>"));
        
        return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""{SoapNamespace}"" xmlns:ax=""{AxNamespace}"">
    <soap:Body>
        <ax:read>
            <ax:_entityKeyList>{keysXml}</ax:_entityKeyList>
        </ax:read>
    </soap:Body>
</soap:Envelope>";
    }
    
    public static Customer? ParseCustomerResponse(string response)
    {
        var doc = XDocument.Parse(response);
        var ns = XNamespace.Get(AxNamespace);
        
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
    
    public static SalesOrder? ParseSalesOrderResponse(string response)
    {
        var doc = XDocument.Parse(response);
        var ns = XNamespace.Get(AxNamespace);
        
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
            Currency = salesTable.Element(ns + "CurrencyCode")?.Value ?? "EUR"
        };
        
        // Parse lines
        order.Lines = salesTable.Descendants(ns + "SalesLine")
            .Select(line => new SalesLine
            {
                LineNum = int.TryParse(line.Element(ns + "LineNum")?.Value, out var ln) ? ln : 0,
                ItemId = line.Element(ns + "ItemId")?.Value ?? "",
                ItemName = line.Element(ns + "Name")?.Value ?? "",
                Quantity = decimal.TryParse(line.Element(ns + "SalesQty")?.Value, out var q) ? q : 0,
                UnitPrice = decimal.TryParse(line.Element(ns + "SalesPrice")?.Value, out var p) ? p : 0,
                LineAmount = decimal.TryParse(line.Element(ns + "LineAmount")?.Value, out var la) ? la : 0,
                ReservedQty = decimal.TryParse(line.Element(ns + "ReservedPhysical")?.Value, out var rq) ? rq : 0,
                DeliveredQty = decimal.TryParse(line.Element(ns + "QtyOrdered")?.Value, out var dq) ? dq - (decimal.TryParse(line.Element(ns + "RemainSalesPhysical")?.Value, out var rs) ? rs : 0) : 0
            })
            .ToList();
        
        return order;
    }
    
    private static string MapSalesStatus(string? axStatus) => axStatus switch
    {
        "1" => "Open",
        "2" => "Delivered",
        "3" => "Invoiced",
        "4" => "Cancelled",
        _ => "Unknown"
    };
    
    // ... additional helper methods
}
```

### DI Registration

```csharp
// Add to ServiceCollectionExtensions.cs
services.AddHttpClient<IAifClient, AifClient>()
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        UseDefaultCredentials = true, // Windows Auth
        PreAuthenticate = true
    });
```

---

## Story 2.2: Get Customer Tool - By Account

### Implementation Plan

```
📁 Get Customer Tool
│
├── 1. Create GetCustomerInput
│   └── customer_account, customer_name, include_*
│
├── 2. Create GetCustomerOutput
│   └── Customer data structure
│
├── 3. Create GetCustomerTool
│   └── Extends ToolBase
│
├── 4. Create input validator
│   └── FluentValidation rules
│
└── 5. Unit tests
    └── Test customer lookup
```

### Files to Create

```csharp
// src/GBL.AX2012.MCP.Server/Tools/GetCustomer/GetCustomerInput.cs
namespace GBL.AX2012.MCP.Server.Tools.GetCustomer;

public class GetCustomerInput
{
    public string? CustomerAccount { get; set; }
    public string? CustomerName { get; set; }
    public bool IncludeAddresses { get; set; } = false;
    public bool IncludeContacts { get; set; } = false;
}

// src/GBL.AX2012.MCP.Server/Tools/GetCustomer/GetCustomerOutput.cs
namespace GBL.AX2012.MCP.Server.Tools.GetCustomer;

public class GetCustomerOutput
{
    public string CustomerAccount { get; set; } = "";
    public string Name { get; set; } = "";
    public string Currency { get; set; } = "";
    public decimal CreditLimit { get; set; }
    public decimal CreditUsed { get; set; }
    public string PaymentTerms { get; set; } = "";
    public string PriceGroup { get; set; } = "";
    public List<CustomerAddress>? Addresses { get; set; }
    public List<CustomerContact>? Contacts { get; set; }
}

public class CustomerAddress
{
    public string Type { get; set; } = "";
    public string Street { get; set; } = "";
    public string City { get; set; } = "";
    public string PostalCode { get; set; } = "";
    public string Country { get; set; } = "";
}

public class CustomerContact
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Role { get; set; } = "";
}

// src/GBL.AX2012.MCP.Server/Tools/GetCustomer/GetCustomerInputValidator.cs
namespace GBL.AX2012.MCP.Server.Tools.GetCustomer;

public class GetCustomerInputValidator : AbstractValidator<GetCustomerInput>
{
    public GetCustomerInputValidator()
    {
        RuleFor(x => x)
            .Must(x => !string.IsNullOrEmpty(x.CustomerAccount) || !string.IsNullOrEmpty(x.CustomerName))
            .WithMessage("Either customer_account or customer_name must be provided");
    }
}

// src/GBL.AX2012.MCP.Server/Tools/GetCustomer/GetCustomerTool.cs
namespace GBL.AX2012.MCP.Server.Tools.GetCustomer;

public class GetCustomerTool : ToolBase<GetCustomerInput, GetCustomerOutput>
{
    private readonly IAifClient _aifClient;
    
    public override string Name => "ax_get_customer";
    public override string Description => "Retrieve customer information from AX 2012 by account number or name search";
    
    public GetCustomerTool(
        ILogger<GetCustomerTool> logger,
        IAuditService audit,
        GetCustomerInputValidator validator,
        IAifClient aifClient)
        : base(logger, audit, validator)
    {
        _aifClient = aifClient;
    }
    
    protected override async Task<GetCustomerOutput> ExecuteCoreAsync(
        GetCustomerInput input, 
        ToolContext context, 
        CancellationToken cancellationToken)
    {
        Customer? customer;
        
        if (!string.IsNullOrEmpty(input.CustomerAccount))
        {
            customer = await _aifClient.GetCustomerAsync(input.CustomerAccount, cancellationToken);
            
            if (customer == null)
            {
                throw new AxException("CUST_NOT_FOUND", $"Customer {input.CustomerAccount} not found");
            }
        }
        else
        {
            // This path is handled by Story 2.3 (fuzzy search)
            throw new AxException("INVALID_INPUT", "customer_account is required for direct lookup");
        }
        
        return new GetCustomerOutput
        {
            CustomerAccount = customer.AccountNum,
            Name = customer.Name,
            Currency = customer.Currency,
            CreditLimit = customer.CreditLimit,
            CreditUsed = customer.CreditUsed,
            PaymentTerms = customer.PaymentTerms,
            PriceGroup = customer.PriceGroup
            // Addresses and Contacts loaded if requested (future enhancement)
        };
    }
}
```

---

## Story 2.3: Get Customer Tool - Fuzzy Search

### Implementation Plan

```
📁 Fuzzy Search Extension
│
├── 1. Create FuzzyMatch helper
│   └── Levenshtein distance scoring
│
├── 2. Create SearchCustomersOutput
│   └── List of matches with confidence
│
├── 3. Extend GetCustomerTool
│   └── Handle customer_name parameter
│
└── 4. Unit tests
    └── Test fuzzy matching
```

### Files to Create

```csharp
// src/GBL.AX2012.MCP.AxConnector/Helpers/FuzzyMatch.cs
namespace GBL.AX2012.MCP.AxConnector.Helpers;

public static class FuzzyMatch
{
    public static int Score(string search, string target)
    {
        if (string.IsNullOrEmpty(search) || string.IsNullOrEmpty(target))
            return 0;
        
        search = search.ToLowerInvariant();
        target = target.ToLowerInvariant();
        
        // Exact match
        if (target == search) return 100;
        
        // Contains match
        if (target.Contains(search)) return 90 - (target.Length - search.Length);
        
        // Starts with
        if (target.StartsWith(search)) return 85;
        
        // Levenshtein distance
        var distance = LevenshteinDistance(search, target);
        var maxLen = Math.Max(search.Length, target.Length);
        var similarity = (1 - (double)distance / maxLen) * 100;
        
        return (int)Math.Max(0, similarity);
    }
    
    private static int LevenshteinDistance(string s1, string s2)
    {
        var m = s1.Length;
        var n = s2.Length;
        var d = new int[m + 1, n + 1];
        
        for (var i = 0; i <= m; i++) d[i, 0] = i;
        for (var j = 0; j <= n; j++) d[0, j] = j;
        
        for (var i = 1; i <= m; i++)
        {
            for (var j = 1; j <= n; j++)
            {
                var cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }
        
        return d[m, n];
    }
}

// src/GBL.AX2012.MCP.Server/Tools/GetCustomer/CustomerSearchResult.cs
namespace GBL.AX2012.MCP.Server.Tools.GetCustomer;

public class CustomerSearchResult
{
    public string CustomerAccount { get; set; } = "";
    public string Name { get; set; } = "";
    public int Confidence { get; set; }
}

public class GetCustomerSearchOutput
{
    public List<CustomerSearchResult> Matches { get; set; } = new();
}
```

### Updated GetCustomerTool

```csharp
protected override async Task<object> ExecuteCoreAsync(
    GetCustomerInput input, 
    ToolContext context, 
    CancellationToken cancellationToken)
{
    if (!string.IsNullOrEmpty(input.CustomerAccount))
    {
        // Direct lookup
        var customer = await _aifClient.GetCustomerAsync(input.CustomerAccount, cancellationToken);
        
        if (customer == null)
        {
            throw new AxException("CUST_NOT_FOUND", $"Customer {input.CustomerAccount} not found");
        }
        
        return MapToOutput(customer);
    }
    else if (!string.IsNullOrEmpty(input.CustomerName))
    {
        // Fuzzy search
        var customers = await _aifClient.SearchCustomersAsync(input.CustomerName, 5, cancellationToken);
        
        return new GetCustomerSearchOutput
        {
            Matches = customers.Select(c => new CustomerSearchResult
            {
                CustomerAccount = c.AccountNum,
                Name = c.Name,
                Confidence = c.MatchConfidence
            }).ToList()
        };
    }
    
    throw new AxException("INVALID_INPUT", "Either customer_account or customer_name must be provided");
}
```

---

## Story 2.4: Get Sales Order Tool - By Sales ID

### Files to Create

```csharp
// src/GBL.AX2012.MCP.Server/Tools/GetSalesOrder/GetSalesOrderInput.cs
namespace GBL.AX2012.MCP.Server.Tools.GetSalesOrder;

public class GetSalesOrderInput
{
    public string? SalesId { get; set; }
    public string? CustomerAccount { get; set; }
    public string[]? StatusFilter { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public bool IncludeLines { get; set; } = false;
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 20;
}

// src/GBL.AX2012.MCP.Server/Tools/GetSalesOrder/GetSalesOrderOutput.cs
namespace GBL.AX2012.MCP.Server.Tools.GetSalesOrder;

public class GetSalesOrderOutput
{
    public string SalesId { get; set; } = "";
    public string CustomerAccount { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public DateTime OrderDate { get; set; }
    public DateTime RequestedDelivery { get; set; }
    public string Status { get; set; } = "";
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "";
    public List<SalesLineOutput>? Lines { get; set; }
}

public class SalesLineOutput
{
    public int LineNum { get; set; }
    public string ItemId { get; set; } = "";
    public string ItemName { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineAmount { get; set; }
    public decimal ReservedQty { get; set; }
    public decimal DeliveredQty { get; set; }
    public string Status { get; set; } = "";
}

// src/GBL.AX2012.MCP.Server/Tools/GetSalesOrder/GetSalesOrderTool.cs
namespace GBL.AX2012.MCP.Server.Tools.GetSalesOrder;

public class GetSalesOrderTool : ToolBase<GetSalesOrderInput, object>
{
    private readonly IAifClient _aifClient;
    
    public override string Name => "ax_get_salesorder";
    public override string Description => "Retrieve sales order information from AX 2012";
    
    public GetSalesOrderTool(
        ILogger<GetSalesOrderTool> logger,
        IAuditService audit,
        GetSalesOrderInputValidator validator,
        IAifClient aifClient)
        : base(logger, audit, validator)
    {
        _aifClient = aifClient;
    }
    
    protected override async Task<object> ExecuteCoreAsync(
        GetSalesOrderInput input, 
        ToolContext context, 
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(input.SalesId))
        {
            // Single order lookup
            var order = await _aifClient.GetSalesOrderAsync(input.SalesId, cancellationToken);
            
            if (order == null)
            {
                throw new AxException("ORDER_NOT_FOUND", $"Sales order {input.SalesId} not found");
            }
            
            return MapToOutput(order, input.IncludeLines);
        }
        else if (!string.IsNullOrEmpty(input.CustomerAccount))
        {
            // Orders by customer
            var filter = new SalesOrderFilter
            {
                StatusFilter = input.StatusFilter,
                DateFrom = input.DateFrom,
                DateTo = input.DateTo,
                Skip = input.Skip,
                Take = input.Take
            };
            
            var orders = await _aifClient.GetSalesOrdersByCustomerAsync(
                input.CustomerAccount, filter, cancellationToken);
            
            return new GetSalesOrderListOutput
            {
                Orders = orders.Select(o => MapToOutput(o, input.IncludeLines)).ToList(),
                Skip = input.Skip,
                Take = input.Take,
                HasMore = orders.Count() == input.Take
            };
        }
        
        throw new AxException("INVALID_INPUT", "Either sales_id or customer_account must be provided");
    }
    
    private GetSalesOrderOutput MapToOutput(SalesOrder order, bool includeLines)
    {
        var output = new GetSalesOrderOutput
        {
            SalesId = order.SalesId,
            CustomerAccount = order.CustomerAccount,
            CustomerName = order.CustomerName,
            OrderDate = order.OrderDate,
            RequestedDelivery = order.RequestedDelivery,
            Status = order.Status,
            TotalAmount = order.TotalAmount,
            Currency = order.Currency
        };
        
        if (includeLines && order.Lines != null)
        {
            output.Lines = order.Lines.Select(l => new SalesLineOutput
            {
                LineNum = l.LineNum,
                ItemId = l.ItemId,
                ItemName = l.ItemName,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                LineAmount = l.LineAmount,
                ReservedQty = l.ReservedQty,
                DeliveredQty = l.DeliveredQty,
                Status = CalculateLineStatus(l)
            }).ToList();
        }
        
        return output;
    }
    
    private string CalculateLineStatus(SalesLine line)
    {
        if (line.DeliveredQty >= line.Quantity) return "Delivered";
        if (line.DeliveredQty > 0) return "Partially Delivered";
        if (line.ReservedQty >= line.Quantity) return "Reserved";
        if (line.ReservedQty > 0) return "Partially Reserved";
        return "Open";
    }
}
```

---

## Story 2.5 & 2.6: Sales Order With Lines & By Customer

Already covered in Story 2.4 implementation above with `IncludeLines` flag and `CustomerAccount` parameter.

---

## Epic 2 Summary

| Story | Files | Tests | Status |
|-------|-------|-------|--------|
| 2.1 | IAifClient, AifClient, SoapHelper | Integration tests | Ready |
| 2.2 | GetCustomerTool, Input, Output, Validator | 3 unit tests | Ready |
| 2.3 | FuzzyMatch, SearchOutput | 2 unit tests | Ready |
| 2.4 | GetSalesOrderTool, Input, Output | 3 unit tests | Ready |
| 2.5 | (Included in 2.4) | 1 unit test | Ready |
| 2.6 | (Included in 2.4) | 2 unit tests | Ready |

**Total:** ~15 files, ~12 unit tests
