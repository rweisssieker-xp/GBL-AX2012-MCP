using FluentValidation;
using Microsoft.Extensions.Logging;
using GBL.AX2012.MCP.Core.Exceptions;
using GBL.AX2012.MCP.Core.Interfaces;
using GBL.AX2012.MCP.Core.Models;
using GBL.AX2012.MCP.AxConnector.Interfaces;

namespace GBL.AX2012.MCP.Server.Tools;

public class CreateSalesOrderInput
{
    public string CustomerAccount { get; set; } = "";
    public DateTime? RequestedDelivery { get; set; }
    public string? CustomerRef { get; set; }
    public List<CreateSalesLineInput> Lines { get; set; } = new();
    public string IdempotencyKey { get; set; } = "";
}

public class CreateSalesLineInput
{
    public string ItemId { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal? UnitPrice { get; set; }
    public string? Warehouse { get; set; }
}

public class CreateSalesOrderOutput
{
    public bool Success { get; set; }
    public string SalesId { get; set; } = "";
    public string CustomerAccount { get; set; } = "";
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "";
    public int LinesCreated { get; set; }
    public List<string> Warnings { get; set; } = new();
    public string AuditId { get; set; } = "";
    public bool Duplicate { get; set; } = false;
}

public class CreateSalesOrderInputValidator : AbstractValidator<CreateSalesOrderInput>
{
    public CreateSalesOrderInputValidator()
    {
        RuleFor(x => x.CustomerAccount)
            .NotEmpty()
            .WithMessage("customer_account is required");
        
        RuleFor(x => x.Lines)
            .NotEmpty()
            .WithMessage("At least one line is required");
        
        RuleFor(x => x.IdempotencyKey)
            .NotEmpty()
            .WithMessage("idempotency_key is required")
            .Must(BeValidGuid)
            .WithMessage("idempotency_key must be a valid UUID");
        
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ItemId)
                .NotEmpty()
                .WithMessage("item_id is required for each line");
            
            line.RuleFor(l => l.Quantity)
                .GreaterThan(0)
                .WithMessage("quantity must be greater than 0");
        });
    }
    
    private bool BeValidGuid(string value)
    {
        return Guid.TryParse(value, out _);
    }
}

public class CreateSalesOrderTool : ToolBase<CreateSalesOrderInput, CreateSalesOrderOutput>
{
    private readonly IWcfClient _wcfClient;
    private readonly IAifClient _aifClient;
    private readonly IIdempotencyStore _idempotencyStore;
    
    public override string Name => "ax_create_salesorder";
    public override string Description => "Create a new sales order in AX 2012";
    
    public CreateSalesOrderTool(
        ILogger<CreateSalesOrderTool> logger,
        IAuditService audit,
        CreateSalesOrderInputValidator validator,
        IWcfClient wcfClient,
        IAifClient aifClient,
        IIdempotencyStore idempotencyStore)
        : base(logger, audit, validator)
    {
        _wcfClient = wcfClient;
        _aifClient = aifClient;
        _idempotencyStore = idempotencyStore;
    }
    
    protected override async Task<CreateSalesOrderOutput> ExecuteCoreAsync(
        CreateSalesOrderInput input, 
        ToolContext context, 
        CancellationToken cancellationToken)
    {
        // 1. Check idempotency
        var existing = await _idempotencyStore.GetAsync<CreateSalesOrderOutput>(input.IdempotencyKey, cancellationToken);
        if (existing != null)
        {
            _logger.LogInformation("Returning cached result for idempotency key {Key}", input.IdempotencyKey);
            existing.Duplicate = true;
            return existing;
        }
        
        // 2. Validate customer
        var customer = await _aifClient.GetCustomerAsync(input.CustomerAccount, cancellationToken);
        if (customer == null)
        {
            throw new AxException("CUST_NOT_FOUND", $"Customer {input.CustomerAccount} not found");
        }
        
        if (customer.Blocked)
        {
            throw new AxException("CUST_BLOCKED", $"Customer {input.CustomerAccount} is blocked for orders");
        }
        
        // 3. Validate items and calculate total
        decimal totalAmount = 0;
        var warnings = new List<string>();
        
        foreach (var line in input.Lines)
        {
            var item = await _aifClient.GetItemAsync(line.ItemId, cancellationToken);
            if (item == null)
            {
                throw new AxException("ITEM_NOT_FOUND", $"Item {line.ItemId} not found");
            }
            
            if (item.BlockedForSales)
            {
                throw new AxException("ITEM_BLOCKED", $"Item {line.ItemId} is blocked for sales");
            }
            
            // Check inventory
            var inventory = await _aifClient.GetInventoryOnHandAsync(line.ItemId, line.Warehouse, cancellationToken);
            if (inventory.Available < line.Quantity)
            {
                warnings.Add($"Item {line.ItemId}: requested {line.Quantity}, available {inventory.Available}");
            }
            
            // Calculate price
            if (line.UnitPrice.HasValue)
            {
                totalAmount += line.UnitPrice.Value * line.Quantity;
            }
            else
            {
                var price = await _aifClient.SimulatePriceAsync(
                    input.CustomerAccount, line.ItemId, line.Quantity, cancellationToken: cancellationToken);
                totalAmount += price.LineAmount;
            }
        }
        
        // 4. Check credit
        var creditAvailable = customer.CreditLimit - customer.CreditUsed;
        if (totalAmount > creditAvailable && customer.CreditLimit > 0)
        {
            throw new AxException("CREDIT_EXCEEDED", 
                $"Order total {totalAmount:N2} {customer.Currency} exceeds available credit {creditAvailable:N2} {customer.Currency}");
        }
        
        // 5. Create order via WCF
        var salesId = await _wcfClient.CreateSalesOrderAsync(new CreateSalesOrderRequest
        {
            CustomerAccount = input.CustomerAccount,
            RequestedDeliveryDate = input.RequestedDelivery ?? DateTime.Today.AddDays(7),
            CustomerRef = input.CustomerRef,
            Lines = input.Lines.Select(l => new CreateSalesLineRequest
            {
                ItemId = l.ItemId,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                WarehouseId = l.Warehouse
            }).ToList()
        }, cancellationToken);
        
        // 6. Build response
        var output = new CreateSalesOrderOutput
        {
            Success = true,
            SalesId = salesId,
            CustomerAccount = input.CustomerAccount,
            OrderDate = DateTime.Today,
            TotalAmount = totalAmount,
            Currency = customer.Currency,
            LinesCreated = input.Lines.Count,
            Warnings = warnings,
            AuditId = Guid.NewGuid().ToString()
        };
        
        // 7. Store for idempotency
        await _idempotencyStore.SetAsync(input.IdempotencyKey, output, TimeSpan.FromDays(7), cancellationToken);
        
        _logger.LogInformation("Created sales order {SalesId} for customer {Customer}, total {Total} {Currency}", 
            salesId, input.CustomerAccount, totalAmount, customer.Currency);
        
        return output;
    }
}
