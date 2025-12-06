---
epic: 3
title: "Inventory & Pricing"
stories: 4
status: "READY"
project_name: "GBL-AX2012-MCP"
date: "2025-12-06"
---

# Epic 3: Inventory & Pricing - Implementation Plans

## Story 3.1: Check Inventory Tool - Basic

### Implementation Plan

```
📁 Check Inventory Tool
│
├── 1. Create CheckInventoryInput
│   └── item_id, warehouse, include_reservations
│
├── 2. Create CheckInventoryOutput
│   └── Inventory data structure
│
├── 3. Create CheckInventoryTool
│   └── Extends ToolBase
│
└── 4. Unit tests
    └── Test inventory lookup
```

### Files to Create

```csharp
// src/GBL.AX2012.MCP.Server/Tools/CheckInventory/CheckInventoryInput.cs
namespace GBL.AX2012.MCP.Server.Tools.CheckInventory;

public class CheckInventoryInput
{
    public string ItemId { get; set; } = "";
    public string? Warehouse { get; set; }
    public bool IncludeReservations { get; set; } = false;
}

// src/GBL.AX2012.MCP.Server/Tools/CheckInventory/CheckInventoryInputValidator.cs
namespace GBL.AX2012.MCP.Server.Tools.CheckInventory;

public class CheckInventoryInputValidator : AbstractValidator<CheckInventoryInput>
{
    public CheckInventoryInputValidator()
    {
        RuleFor(x => x.ItemId)
            .NotEmpty()
            .WithMessage("item_id is required");
    }
}

// src/GBL.AX2012.MCP.Server/Tools/CheckInventory/CheckInventoryOutput.cs
namespace GBL.AX2012.MCP.Server.Tools.CheckInventory;

public class CheckInventoryOutput
{
    public string ItemId { get; set; } = "";
    public string ItemName { get; set; } = "";
    public decimal TotalOnHand { get; set; }
    public decimal Available { get; set; }
    public decimal Reserved { get; set; }
    public decimal OnOrder { get; set; }
    public List<WarehouseInventory>? Warehouses { get; set; }
}

public class WarehouseInventory
{
    public string WarehouseId { get; set; } = "";
    public string WarehouseName { get; set; } = "";
    public decimal OnHand { get; set; }
    public decimal Available { get; set; }
    public decimal Reserved { get; set; }
}

// src/GBL.AX2012.MCP.Server/Tools/CheckInventory/CheckInventoryTool.cs
namespace GBL.AX2012.MCP.Server.Tools.CheckInventory;

public class CheckInventoryTool : ToolBase<CheckInventoryInput, CheckInventoryOutput>
{
    private readonly IAifClient _aifClient;
    
    public override string Name => "ax_check_inventory";
    public override string Description => "Check inventory availability for an item in AX 2012";
    
    public CheckInventoryTool(
        ILogger<CheckInventoryTool> logger,
        IAuditService audit,
        CheckInventoryInputValidator validator,
        IAifClient aifClient)
        : base(logger, audit, validator)
    {
        _aifClient = aifClient;
    }
    
    protected override async Task<CheckInventoryOutput> ExecuteCoreAsync(
        CheckInventoryInput input, 
        ToolContext context, 
        CancellationToken cancellationToken)
    {
        var inventory = await _aifClient.GetInventoryOnHandAsync(
            input.ItemId, 
            input.Warehouse, 
            cancellationToken);
        
        if (inventory == null || inventory.TotalOnHand == 0 && inventory.Warehouses?.Count == 0)
        {
            // Check if item exists
            var item = await _aifClient.GetItemAsync(input.ItemId, cancellationToken);
            if (item == null)
            {
                throw new AxException("ITEM_NOT_FOUND", $"Item {input.ItemId} not found");
            }
        }
        
        var output = new CheckInventoryOutput
        {
            ItemId = inventory.ItemId,
            ItemName = inventory.ItemName,
            TotalOnHand = inventory.TotalOnHand,
            Available = inventory.Available,
            Reserved = inventory.Reserved,
            OnOrder = inventory.OnOrder
        };
        
        if (input.IncludeReservations && inventory.Warehouses != null)
        {
            output.Warehouses = inventory.Warehouses.Select(w => new WarehouseInventory
            {
                WarehouseId = w.WarehouseId,
                WarehouseName = w.WarehouseName,
                OnHand = w.OnHand,
                Available = w.Available,
                Reserved = w.Reserved
            }).ToList();
        }
        
        return output;
    }
}
```

### Unit Tests

```csharp
// tests/GBL.AX2012.MCP.Server.Tests/CheckInventoryToolTests.cs
public class CheckInventoryToolTests
{
    [Fact]
    public async Task Execute_ValidItem_ReturnsInventory()
    {
        var aifClient = new Mock<IAifClient>();
        aifClient.Setup(x => x.GetInventoryOnHandAsync("WIDGET-PRO", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InventoryOnHand
            {
                ItemId = "WIDGET-PRO",
                ItemName = "Widget Professional",
                TotalOnHand = 500,
                Available = 320,
                Reserved = 180,
                OnOrder = 200
            });
        
        var tool = CreateTool(aifClient.Object);
        var input = JsonSerializer.SerializeToElement(new CheckInventoryInput { ItemId = "WIDGET-PRO" });
        
        var result = await tool.ExecuteAsync(input, new ToolContext(), CancellationToken.None);
        
        Assert.True(result.Success);
        var output = (CheckInventoryOutput)result.Data!;
        Assert.Equal(500, output.TotalOnHand);
        Assert.Equal(320, output.Available);
    }
    
    [Fact]
    public async Task Execute_ItemNotFound_ThrowsException()
    {
        var aifClient = new Mock<IAifClient>();
        aifClient.Setup(x => x.GetInventoryOnHandAsync("INVALID", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InventoryOnHand());
        aifClient.Setup(x => x.GetItemAsync("INVALID", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Item?)null);
        
        var tool = CreateTool(aifClient.Object);
        var input = JsonSerializer.SerializeToElement(new CheckInventoryInput { ItemId = "INVALID" });
        
        var result = await tool.ExecuteAsync(input, new ToolContext(), CancellationToken.None);
        
        Assert.False(result.Success);
        Assert.Equal("ITEM_NOT_FOUND", result.ErrorCode);
    }
}
```

---

## Story 3.2: Check Inventory Tool - By Warehouse

Already covered in Story 3.1 with `Warehouse` parameter and `IncludeReservations` flag.

Additional test:

```csharp
[Fact]
public async Task Execute_WithWarehouse_ReturnsWarehouseBreakdown()
{
    var aifClient = new Mock<IAifClient>();
    aifClient.Setup(x => x.GetInventoryOnHandAsync("WIDGET-PRO", null, It.IsAny<CancellationToken>()))
        .ReturnsAsync(new InventoryOnHand
        {
            ItemId = "WIDGET-PRO",
            ItemName = "Widget Professional",
            TotalOnHand = 500,
            Available = 320,
            Reserved = 180,
            Warehouses = new List<WarehouseInventoryData>
            {
                new() { WarehouseId = "WH-MAIN", OnHand = 400, Available = 250, Reserved = 150 },
                new() { WarehouseId = "WH-EAST", OnHand = 100, Available = 70, Reserved = 30 }
            }
        });
    
    var tool = CreateTool(aifClient.Object);
    var input = JsonSerializer.SerializeToElement(new CheckInventoryInput 
    { 
        ItemId = "WIDGET-PRO",
        IncludeReservations = true
    });
    
    var result = await tool.ExecuteAsync(input, new ToolContext(), CancellationToken.None);
    
    Assert.True(result.Success);
    var output = (CheckInventoryOutput)result.Data!;
    Assert.NotNull(output.Warehouses);
    Assert.Equal(2, output.Warehouses.Count);
    Assert.Equal("WH-MAIN", output.Warehouses[0].WarehouseId);
}
```

---

## Story 3.3: Simulate Price Tool - Basic

### Implementation Plan

```
📁 Simulate Price Tool
│
├── 1. Create SimulatePriceInput
│   └── customer_account, item_id, quantity, unit, date
│
├── 2. Create SimulatePriceOutput
│   └── Price breakdown structure
│
├── 3. Create SimulatePriceTool
│   └── Extends ToolBase
│
└── 4. Unit tests
    └── Test price simulation
```

### Files to Create

```csharp
// src/GBL.AX2012.MCP.Server/Tools/SimulatePrice/SimulatePriceInput.cs
namespace GBL.AX2012.MCP.Server.Tools.SimulatePrice;

public class SimulatePriceInput
{
    public string CustomerAccount { get; set; } = "";
    public string ItemId { get; set; } = "";
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }
    public DateTime? Date { get; set; }
}

// src/GBL.AX2012.MCP.Server/Tools/SimulatePrice/SimulatePriceInputValidator.cs
namespace GBL.AX2012.MCP.Server.Tools.SimulatePrice;

public class SimulatePriceInputValidator : AbstractValidator<SimulatePriceInput>
{
    public SimulatePriceInputValidator()
    {
        RuleFor(x => x.CustomerAccount)
            .NotEmpty()
            .WithMessage("customer_account is required");
        
        RuleFor(x => x.ItemId)
            .NotEmpty()
            .WithMessage("item_id is required");
        
        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("quantity must be greater than 0");
    }
}

// src/GBL.AX2012.MCP.Server/Tools/SimulatePrice/SimulatePriceOutput.cs
namespace GBL.AX2012.MCP.Server.Tools.SimulatePrice;

public class SimulatePriceOutput
{
    public string CustomerAccount { get; set; } = "";
    public string ItemId { get; set; } = "";
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = "PCS";
    public decimal BasePrice { get; set; }
    public decimal CustomerDiscountPct { get; set; }
    public decimal QuantityDiscountPct { get; set; }
    public decimal FinalUnitPrice { get; set; }
    public decimal LineAmount { get; set; }
    public string Currency { get; set; } = "EUR";
    public string PriceSource { get; set; } = "";
    public DateTime? ValidUntil { get; set; }
}

// src/GBL.AX2012.MCP.Server/Tools/SimulatePrice/SimulatePriceTool.cs
namespace GBL.AX2012.MCP.Server.Tools.SimulatePrice;

public class SimulatePriceTool : ToolBase<SimulatePriceInput, SimulatePriceOutput>
{
    private readonly IAifClient _aifClient;
    
    public override string Name => "ax_simulate_price";
    public override string Description => "Simulate pricing for a customer/item combination without creating an order";
    
    public SimulatePriceTool(
        ILogger<SimulatePriceTool> logger,
        IAuditService audit,
        SimulatePriceInputValidator validator,
        IAifClient aifClient)
        : base(logger, audit, validator)
    {
        _aifClient = aifClient;
    }
    
    protected override async Task<SimulatePriceOutput> ExecuteCoreAsync(
        SimulatePriceInput input, 
        ToolContext context, 
        CancellationToken cancellationToken)
    {
        // Validate customer exists
        var customer = await _aifClient.GetCustomerAsync(input.CustomerAccount, cancellationToken);
        if (customer == null)
        {
            throw new AxException("CUST_NOT_FOUND", $"Customer {input.CustomerAccount} not found");
        }
        
        // Validate item exists
        var item = await _aifClient.GetItemAsync(input.ItemId, cancellationToken);
        if (item == null)
        {
            throw new AxException("ITEM_NOT_FOUND", $"Item {input.ItemId} not found");
        }
        
        // Simulate price
        var priceResult = await _aifClient.SimulatePriceAsync(
            input.CustomerAccount,
            input.ItemId,
            input.Quantity,
            input.Date,
            cancellationToken);
        
        if (priceResult == null || priceResult.FinalUnitPrice == 0)
        {
            throw new AxException("NO_VALID_PRICE", 
                $"No valid price found for customer {input.CustomerAccount} and item {input.ItemId}");
        }
        
        return new SimulatePriceOutput
        {
            CustomerAccount = input.CustomerAccount,
            ItemId = input.ItemId,
            Quantity = input.Quantity,
            Unit = input.Unit ?? "PCS",
            BasePrice = priceResult.BasePrice,
            CustomerDiscountPct = priceResult.CustomerDiscountPct,
            QuantityDiscountPct = priceResult.QuantityDiscountPct,
            FinalUnitPrice = priceResult.FinalUnitPrice,
            LineAmount = priceResult.LineAmount,
            Currency = priceResult.Currency,
            PriceSource = priceResult.PriceSource,
            ValidUntil = priceResult.ValidUntil
        };
    }
}
```

### Unit Tests

```csharp
// tests/GBL.AX2012.MCP.Server.Tests/SimulatePriceToolTests.cs
public class SimulatePriceToolTests
{
    [Fact]
    public async Task Execute_ValidInput_ReturnsPriceBreakdown()
    {
        var aifClient = new Mock<IAifClient>();
        aifClient.Setup(x => x.GetCustomerAsync("CUST-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Customer { AccountNum = "CUST-001" });
        aifClient.Setup(x => x.GetItemAsync("WIDGET-PRO", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Item { ItemId = "WIDGET-PRO" });
        aifClient.Setup(x => x.SimulatePriceAsync("CUST-001", "WIDGET-PRO", 50, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PriceResult
            {
                BasePrice = 120.00m,
                CustomerDiscountPct = 10.0m,
                QuantityDiscountPct = 5.0m,
                FinalUnitPrice = 102.60m,
                LineAmount = 5130.00m,
                Currency = "EUR",
                PriceSource = "Trade Agreement",
                ValidUntil = new DateTime(2025, 12, 31)
            });
        
        var tool = CreateTool(aifClient.Object);
        var input = JsonSerializer.SerializeToElement(new SimulatePriceInput
        {
            CustomerAccount = "CUST-001",
            ItemId = "WIDGET-PRO",
            Quantity = 50
        });
        
        var result = await tool.ExecuteAsync(input, new ToolContext(), CancellationToken.None);
        
        Assert.True(result.Success);
        var output = (SimulatePriceOutput)result.Data!;
        Assert.Equal(120.00m, output.BasePrice);
        Assert.Equal(102.60m, output.FinalUnitPrice);
        Assert.Equal(5130.00m, output.LineAmount);
        Assert.Equal("Trade Agreement", output.PriceSource);
    }
    
    [Fact]
    public async Task Execute_CustomerNotFound_ThrowsException()
    {
        var aifClient = new Mock<IAifClient>();
        aifClient.Setup(x => x.GetCustomerAsync("INVALID", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);
        
        var tool = CreateTool(aifClient.Object);
        var input = JsonSerializer.SerializeToElement(new SimulatePriceInput
        {
            CustomerAccount = "INVALID",
            ItemId = "WIDGET-PRO",
            Quantity = 50
        });
        
        var result = await tool.ExecuteAsync(input, new ToolContext(), CancellationToken.None);
        
        Assert.False(result.Success);
        Assert.Equal("CUST_NOT_FOUND", result.ErrorCode);
    }
    
    [Fact]
    public async Task Execute_NoPriceFound_ThrowsException()
    {
        var aifClient = new Mock<IAifClient>();
        aifClient.Setup(x => x.GetCustomerAsync("CUST-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Customer { AccountNum = "CUST-001" });
        aifClient.Setup(x => x.GetItemAsync("WIDGET-PRO", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Item { ItemId = "WIDGET-PRO" });
        aifClient.Setup(x => x.SimulatePriceAsync("CUST-001", "WIDGET-PRO", 50, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PriceResult { FinalUnitPrice = 0 });
        
        var tool = CreateTool(aifClient.Object);
        var input = JsonSerializer.SerializeToElement(new SimulatePriceInput
        {
            CustomerAccount = "CUST-001",
            ItemId = "WIDGET-PRO",
            Quantity = 50
        });
        
        var result = await tool.ExecuteAsync(input, new ToolContext(), CancellationToken.None);
        
        Assert.False(result.Success);
        Assert.Equal("NO_VALID_PRICE", result.ErrorCode);
    }
}
```

---

## Story 3.4: Simulate Price Tool - Date Override

Already covered in Story 3.3 with `Date` parameter.

Additional test:

```csharp
[Fact]
public async Task Execute_WithFutureDate_UsesFuturePricing()
{
    var futureDate = new DateTime(2026, 1, 15);
    var aifClient = new Mock<IAifClient>();
    aifClient.Setup(x => x.GetCustomerAsync("CUST-001", It.IsAny<CancellationToken>()))
        .ReturnsAsync(new Customer { AccountNum = "CUST-001" });
    aifClient.Setup(x => x.GetItemAsync("WIDGET-PRO", It.IsAny<CancellationToken>()))
        .ReturnsAsync(new Item { ItemId = "WIDGET-PRO" });
    aifClient.Setup(x => x.SimulatePriceAsync("CUST-001", "WIDGET-PRO", 50, futureDate, It.IsAny<CancellationToken>()))
        .ReturnsAsync(new PriceResult
        {
            BasePrice = 130.00m, // Different price for future date
            FinalUnitPrice = 110.50m,
            LineAmount = 5525.00m,
            Currency = "EUR",
            PriceSource = "Future Trade Agreement",
            ValidUntil = new DateTime(2026, 6, 30)
        });
    
    var tool = CreateTool(aifClient.Object);
    var input = JsonSerializer.SerializeToElement(new SimulatePriceInput
    {
        CustomerAccount = "CUST-001",
        ItemId = "WIDGET-PRO",
        Quantity = 50,
        Date = futureDate
    });
    
    var result = await tool.ExecuteAsync(input, new ToolContext(), CancellationToken.None);
    
    Assert.True(result.Success);
    var output = (SimulatePriceOutput)result.Data!;
    Assert.Equal(130.00m, output.BasePrice);
    Assert.Equal("Future Trade Agreement", output.PriceSource);
}
```

---

## Epic 3 Summary

| Story | Files | Tests | Status |
|-------|-------|-------|--------|
| 3.1 | CheckInventoryTool, Input, Output, Validator | 2 unit tests | Ready |
| 3.2 | (Included in 3.1) | 1 unit test | Ready |
| 3.3 | SimulatePriceTool, Input, Output, Validator | 3 unit tests | Ready |
| 3.4 | (Included in 3.3) | 1 unit test | Ready |

**Total:** ~10 files, ~7 unit tests
