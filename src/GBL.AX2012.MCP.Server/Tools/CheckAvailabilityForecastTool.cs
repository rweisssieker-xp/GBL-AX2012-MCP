using FluentValidation;
using Microsoft.Extensions.Logging;
using GBL.AX2012.MCP.Core.Interfaces;
using GBL.AX2012.MCP.Core.Models;
using GBL.AX2012.MCP.AxConnector.Interfaces;

namespace GBL.AX2012.MCP.Server.Tools;

public class CheckAvailabilityForecastInput
{
    public string ItemId { get; set; } = "";
    public decimal RequiredQuantity { get; set; }
    public string? WarehouseId { get; set; }
    public int ForecastDays { get; set; } = 30;
}

public class CheckAvailabilityForecastOutput
{
    public string ItemId { get; set; } = "";
    public decimal CurrentStock { get; set; }
    public decimal RequiredQuantity { get; set; }
    public bool IsAvailableNow { get; set; }
    public DateTime? ExpectedAvailabilityDate { get; set; }
    public List<IncomingSupply> IncomingSupplies { get; set; } = new();
    public List<OutgoingDemand> OutgoingDemands { get; set; } = new();
    public string Recommendation { get; set; } = "";
}

public class IncomingSupply
{
    public string Type { get; set; } = ""; // PurchaseOrder, ProductionOrder, Transfer
    public string ReferenceId { get; set; } = "";
    public decimal Quantity { get; set; }
    public DateTime ExpectedDate { get; set; }
    public string Status { get; set; } = "";
}

public class OutgoingDemand
{
    public string Type { get; set; } = ""; // SalesOrder, Transfer, Production
    public string ReferenceId { get; set; } = "";
    public decimal Quantity { get; set; }
    public DateTime RequiredDate { get; set; }
    public bool IsReserved { get; set; }
}

public class CheckAvailabilityForecastInputValidator : AbstractValidator<CheckAvailabilityForecastInput>
{
    public CheckAvailabilityForecastInputValidator()
    {
        RuleFor(x => x.ItemId).NotEmpty().MaximumLength(20);
        RuleFor(x => x.RequiredQuantity).GreaterThan(0);
        RuleFor(x => x.ForecastDays).InclusiveBetween(1, 365);
    }
}

public class CheckAvailabilityForecastTool : ToolBase<CheckAvailabilityForecastInput, CheckAvailabilityForecastOutput>
{
    private readonly IAifClient _aifClient;
    
    public override string Name => "ax_check_availability_forecast";
    public override string Description => "Check when an item will be available based on incoming supply and outgoing demand";
    
    public CheckAvailabilityForecastTool(
        ILogger<CheckAvailabilityForecastTool> logger,
        IAuditService audit,
        CheckAvailabilityForecastInputValidator validator,
        IAifClient aifClient)
        : base(logger, audit, validator)
    {
        _aifClient = aifClient;
    }
    
    protected override async Task<CheckAvailabilityForecastOutput> ExecuteCoreAsync(
        CheckAvailabilityForecastInput input, 
        ToolContext context, 
        CancellationToken cancellationToken)
    {
        // Get current inventory
        var inventory = await _aifClient.GetInventoryOnHandAsync(input.ItemId, input.WarehouseId);
        var currentStock = inventory?.AvailablePhysical ?? 0;
        
        // Simulate forecast data (in real implementation, query AX for PO/SO/Production orders)
        var forecastEnd = DateTime.Today.AddDays(input.ForecastDays);
        var incomingSupplies = await GetIncomingSuppliesAsync(input.ItemId, input.WarehouseId, forecastEnd);
        var outgoingDemands = await GetOutgoingDemandsAsync(input.ItemId, input.WarehouseId, forecastEnd);
        
        // Calculate when required quantity will be available
        var isAvailableNow = currentStock >= input.RequiredQuantity;
        DateTime? expectedDate = null;
        
        if (!isAvailableNow)
        {
            var runningStock = currentStock;
            var allEvents = incomingSupplies
                .Select(s => new { Date = s.ExpectedDate, Delta = s.Quantity })
                .Concat(outgoingDemands.Where(d => !d.IsReserved).Select(d => new { Date = d.RequiredDate, Delta = -d.Quantity }))
                .OrderBy(e => e.Date)
                .ToList();
            
            foreach (var evt in allEvents)
            {
                runningStock += evt.Delta;
                if (runningStock >= input.RequiredQuantity)
                {
                    expectedDate = evt.Date;
                    break;
                }
            }
        }
        
        // Generate recommendation
        var recommendation = GenerateRecommendation(isAvailableNow, expectedDate, input.RequiredQuantity, currentStock, incomingSupplies);
        
        return new CheckAvailabilityForecastOutput
        {
            ItemId = input.ItemId,
            CurrentStock = currentStock,
            RequiredQuantity = input.RequiredQuantity,
            IsAvailableNow = isAvailableNow,
            ExpectedAvailabilityDate = isAvailableNow ? DateTime.Today : expectedDate,
            IncomingSupplies = incomingSupplies,
            OutgoingDemands = outgoingDemands,
            Recommendation = recommendation
        };
    }
    
    private Task<List<IncomingSupply>> GetIncomingSuppliesAsync(string itemId, string? warehouseId, DateTime until)
    {
        // Simulated - in real implementation, query AX PurchTable, ProdTable, InventTransferTable
        var supplies = new List<IncomingSupply>
        {
            new() { Type = "PurchaseOrder", ReferenceId = "PO-001234", Quantity = 100, ExpectedDate = DateTime.Today.AddDays(5), Status = "Confirmed" },
            new() { Type = "ProductionOrder", ReferenceId = "PROD-005678", Quantity = 50, ExpectedDate = DateTime.Today.AddDays(10), Status = "Released" }
        };
        return Task.FromResult(supplies);
    }
    
    private Task<List<OutgoingDemand>> GetOutgoingDemandsAsync(string itemId, string? warehouseId, DateTime until)
    {
        // Simulated - in real implementation, query AX SalesTable, InventTransferTable
        var demands = new List<OutgoingDemand>
        {
            new() { Type = "SalesOrder", ReferenceId = "SO-001111", Quantity = 30, RequiredDate = DateTime.Today.AddDays(2), IsReserved = true },
            new() { Type = "SalesOrder", ReferenceId = "SO-001112", Quantity = 20, RequiredDate = DateTime.Today.AddDays(7), IsReserved = false }
        };
        return Task.FromResult(demands);
    }
    
    private string GenerateRecommendation(bool isAvailableNow, DateTime? expectedDate, decimal required, decimal current, List<IncomingSupply> supplies)
    {
        if (isAvailableNow)
            return $"Item is available now. Current stock ({current}) covers required quantity ({required}).";
        
        if (expectedDate.HasValue)
            return $"Item will be available on {expectedDate:yyyy-MM-dd}. Gap: {required - current} units. Next supply: {supplies.FirstOrDefault()?.ReferenceId ?? "none"}.";
        
        return $"Item may not be available within forecast period. Consider expediting purchase orders or finding alternative items.";
    }
}
