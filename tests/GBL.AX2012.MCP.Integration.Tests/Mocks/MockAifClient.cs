using GBL.AX2012.MCP.AxConnector.Interfaces;
using GBL.AX2012.MCP.Core.Models;

namespace GBL.AX2012.MCP.Integration.Tests.Mocks;

public class MockAifClient : IAifClient
{
    private readonly Dictionary<string, Customer> _customers = new()
    {
        ["CUST-001"] = new Customer
        {
            AccountNum = "CUST-001",
            Name = "Müller GmbH",
            Currency = "EUR",
            CreditLimit = 100000,
            CreditUsed = 25000,
            PaymentTerms = "Net30",
            PriceGroup = "RETAIL",
            Blocked = false
        },
        ["CUST-002"] = new Customer
        {
            AccountNum = "CUST-002",
            Name = "Schmidt AG",
            Currency = "EUR",
            CreditLimit = 50000,
            CreditUsed = 48000,
            PaymentTerms = "Net14",
            PriceGroup = "WHOLESALE",
            Blocked = false
        },
        ["CUST-BLOCKED"] = new Customer
        {
            AccountNum = "CUST-BLOCKED",
            Name = "Blocked Customer",
            Currency = "EUR",
            CreditLimit = 10000,
            CreditUsed = 15000,
            Blocked = true
        }
    };
    
    private readonly Dictionary<string, Item> _items = new()
    {
        ["ITEM-100"] = new Item { ItemId = "ITEM-100", Name = "Widget Pro", ItemGroup = "WIDGETS", Unit = "PCS", BlockedForSales = false },
        ["ITEM-200"] = new Item { ItemId = "ITEM-200", Name = "Gadget Plus", ItemGroup = "GADGETS", Unit = "PCS", BlockedForSales = false },
        ["ITEM-BLOCKED"] = new Item { ItemId = "ITEM-BLOCKED", Name = "Blocked Item", ItemGroup = "MISC", Unit = "PCS", BlockedForSales = true }
    };
    
    private readonly Dictionary<string, SalesOrder> _orders = new()
    {
        ["SO-2024-001"] = new SalesOrder
        {
            SalesId = "SO-2024-001",
            CustomerAccount = "CUST-001",
            CustomerName = "Müller GmbH",
            OrderDate = DateTime.Today.AddDays(-10),
            RequestedDelivery = DateTime.Today.AddDays(5),
            Status = "Open",
            Currency = "EUR",
            TotalAmount = 10000,
            Lines = new List<SalesLine>
            {
                new SalesLine
                {
                    LineNum = 1,
                    ItemId = "ITEM-100",
                    ItemName = "Widget Pro",
                    Quantity = 50,
                    UnitPrice = 200,
                    LineAmount = 10000,
                    ReservedQty = 0,
                    DeliveredQty = 0
                }
            }
        }
    };
    
    public Task<Customer?> GetCustomerAsync(string accountNum, CancellationToken cancellationToken = default)
    {
        _customers.TryGetValue(accountNum, out var customer);
        return Task.FromResult(customer);
    }
    
    public Task<IEnumerable<Customer>> SearchCustomersAsync(string name, int maxResults = 10, CancellationToken cancellationToken = default)
    {
        var matches = _customers.Values
            .Where(c => c.Name.Contains(name, StringComparison.OrdinalIgnoreCase))
            .Take(maxResults)
            .Select(c => c with { MatchConfidence = 85 });
        return Task.FromResult(matches);
    }
    
    public Task<Item?> GetItemAsync(string itemId, CancellationToken cancellationToken = default)
    {
        _items.TryGetValue(itemId, out var item);
        return Task.FromResult(item);
    }
    
    public Task<SalesOrder?> GetSalesOrderAsync(string salesId, CancellationToken cancellationToken = default)
    {
        _orders.TryGetValue(salesId, out var order);
        return Task.FromResult(order);
    }
    
    public Task<IEnumerable<SalesOrder>> GetSalesOrdersByCustomerAsync(string customerAccount, SalesOrderFilter? filter = null, CancellationToken cancellationToken = default)
    {
        var orders = _orders.Values.Where(o => o.CustomerAccount == customerAccount);
        
        if (filter?.StatusFilter?.Any() == true)
            orders = orders.Where(o => filter.StatusFilter.Contains(o.Status));
        
        return Task.FromResult(orders.Take(filter?.Take ?? 20));
    }
    
    public Task<InventoryOnHand> GetInventoryOnHandAsync(string itemId, string? warehouse = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new InventoryOnHand
        {
            ItemId = itemId,
            TotalOnHand = 500,
            Available = 450,
            Reserved = 50,
            OnOrder = 100,
            Warehouses = new List<WarehouseInventory>
            {
                new WarehouseInventory
                {
                    WarehouseId = warehouse ?? "WH-MAIN",
                    WarehouseName = "Main Warehouse",
                    OnHand = 500,
                    Available = 450,
                    Reserved = 50
                }
            }
        });
    }
    
    public Task<PriceResult> SimulatePriceAsync(string customerAccount, string itemId, decimal quantity, DateTime? date = null, CancellationToken cancellationToken = default)
    {
        var basePrice = itemId == "ITEM-100" ? 200m : 150m;
        var customerDiscount = customerAccount == "CUST-001" ? 5m : 0m;
        var qtyDiscount = quantity >= 50 ? 3m : 0m;
        var finalPrice = basePrice * (1 - (customerDiscount + qtyDiscount) / 100);
        
        return Task.FromResult(new PriceResult
        {
            BasePrice = basePrice,
            CustomerDiscountPct = customerDiscount,
            QuantityDiscountPct = qtyDiscount,
            FinalUnitPrice = finalPrice,
            LineAmount = finalPrice * quantity,
            Currency = "EUR",
            PriceSource = "TradeAgreement",
            ValidUntil = DateTime.Today.AddMonths(1)
        });
    }
    
    public Task<IEnumerable<ReservationQueueEntry>> GetReservationQueueAsync(string itemId, string? warehouseId = null, CancellationToken cancellationToken = default)
    {
        var entries = new List<ReservationQueueEntry>
        {
            new ReservationQueueEntry
            {
                SalesId = "SO-2024-001",
                LineNum = 1,
                CustomerAccount = "CUST-001",
                CustomerName = "Müller GmbH",
                ItemId = itemId,
                ReservedQty = 50,
                PendingQty = 0,
                RequestedDate = DateTime.Today.AddDays(5),
                OrderDate = DateTime.Today.AddDays(-10),
                Priority = 1
            },
            new ReservationQueueEntry
            {
                SalesId = "SO-2024-002",
                LineNum = 1,
                CustomerAccount = "CUST-002",
                CustomerName = "Schmidt AG",
                ItemId = itemId,
                ReservedQty = 0,
                PendingQty = 100,
                RequestedDate = DateTime.Today.AddDays(7),
                OrderDate = DateTime.Today.AddDays(-5),
                Priority = 2
            }
        };
        
        return Task.FromResult<IEnumerable<ReservationQueueEntry>>(entries);
    }
}
