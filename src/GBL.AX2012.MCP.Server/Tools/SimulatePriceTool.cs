using FluentValidation;
using Microsoft.Extensions.Logging;
using GBL.AX2012.MCP.Core.Exceptions;
using GBL.AX2012.MCP.Core.Interfaces;
using GBL.AX2012.MCP.Core.Models;
using GBL.AX2012.MCP.AxConnector.Interfaces;

namespace GBL.AX2012.MCP.Server.Tools;

public class SimulatePriceInput
{
    public string CustomerAccount { get; set; } = "";
    public string ItemId { get; set; } = "";
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }
    public DateTime? Date { get; set; }
}

public class SimulatePriceOutput
{
    public string CustomerAccount { get; set; } = "";
    public string ItemId { get; set; } = "";
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = "PCS";
    public decimal BasePrice { get; set; }
    public decimal CustomerDiscountPct { get; set; }
    public decimal QuantityDiscountPct { get; set; }
    public decimal TotalDiscountPct => CustomerDiscountPct + QuantityDiscountPct;
    public decimal FinalUnitPrice { get; set; }
    public decimal LineAmount { get; set; }
    public string Currency { get; set; } = "EUR";
    public string PriceSource { get; set; } = "";
    public DateTime? ValidUntil { get; set; }
}

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
        
        if (priceResult.FinalUnitPrice == 0)
        {
            throw new AxException("NO_VALID_PRICE", 
                $"No valid price found for customer {input.CustomerAccount} and item {input.ItemId}");
        }
        
        return new SimulatePriceOutput
        {
            CustomerAccount = input.CustomerAccount,
            ItemId = input.ItemId,
            Quantity = input.Quantity,
            Unit = input.Unit ?? item.Unit,
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
