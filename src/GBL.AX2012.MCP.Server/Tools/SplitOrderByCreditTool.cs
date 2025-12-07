using FluentValidation;
using Microsoft.Extensions.Logging;
using GBL.AX2012.MCP.Core.Interfaces;
using GBL.AX2012.MCP.Core.Models;
using GBL.AX2012.MCP.AxConnector.Interfaces;

namespace GBL.AX2012.MCP.Server.Tools;

public class SplitOrderByCreditInput
{
    public string SalesId { get; set; } = "";
    public string CustomerAccount { get; set; } = "";
}

public class SplitOrderByCreditOutput
{
    public bool WasSplit { get; set; }
    public string OriginalSalesId { get; set; } = "";
    public string? NewSalesId { get; set; }
    public decimal OriginalOrderAmount { get; set; }
    public decimal? AmountWithinLimit { get; set; }
    public decimal? AmountExceedingLimit { get; set; }
    public decimal CreditLimit { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal AvailableCredit { get; set; }
    public List<SplitLineOutput> SplitLines { get; set; } = new();
    public string? Message { get; set; }
}

public class SplitLineOutput
{
    public int OriginalLineNum { get; set; }
    public int? NewLineNum { get; set; }
    public string ItemId { get; set; } = "";
    public decimal OriginalQty { get; set; }
    public decimal RemainingQty { get; set; }
    public decimal SplitQty { get; set; }
}

public class SplitOrderByCreditInputValidator : AbstractValidator<SplitOrderByCreditInput>
{
    public SplitOrderByCreditInputValidator()
    {
        RuleFor(x => x.SalesId)
            .NotEmpty()
            .WithMessage("SalesId is required");
            
        RuleFor(x => x.CustomerAccount)
            .NotEmpty()
            .WithMessage("CustomerAccount is required");
    }
}

public class SplitOrderByCreditTool : ToolBase<SplitOrderByCreditInput, SplitOrderByCreditOutput>
{
    private readonly IWcfClient _wcfClient;
    private readonly IAifClient _aifClient;
    private readonly SplitOrderByCreditInputValidator _validator;
    
    public override string Name => "ax_split_order_by_credit";
    public override string Description => "Split a sales order when it exceeds credit limit - creates two orders: one within limit, one requiring approval";
    
    public SplitOrderByCreditTool(
        ILogger<SplitOrderByCreditTool> logger,
        IAuditService auditService,
        IWcfClient wcfClient,
        IAifClient aifClient,
        SplitOrderByCreditInputValidator validator)
        : base(logger, auditService)
    {
        _wcfClient = wcfClient;
        _aifClient = aifClient;
        _validator = validator;
    }
    
    protected override async Task<SplitOrderByCreditOutput> ExecuteCoreAsync(
        SplitOrderByCreditInput input,
        ToolContext context,
        CancellationToken cancellationToken)
    {
        var validation = await _validator.ValidateAsync(input, cancellationToken);
        if (!validation.IsValid)
        {
            throw new FluentValidation.ValidationException(validation.Errors);
        }
        
        _logger.LogInformation("Checking credit for order {SalesId}, customer {CustomerAccount}", 
            input.SalesId, input.CustomerAccount);
        
        // Get customer credit info
        var customer = await _aifClient.GetCustomerAsync(input.CustomerAccount, cancellationToken);
        if (customer == null)
        {
            return new SplitOrderByCreditOutput
            {
                WasSplit = false,
                OriginalSalesId = input.SalesId,
                Message = $"Customer {input.CustomerAccount} not found"
            };
        }
        
        // Get order
        var order = await _aifClient.GetSalesOrderAsync(input.SalesId, cancellationToken);
        if (order == null)
        {
            return new SplitOrderByCreditOutput
            {
                WasSplit = false,
                OriginalSalesId = input.SalesId,
                Message = $"Sales order {input.SalesId} not found"
            };
        }
        
        var creditLimit = customer.CreditLimit;
        var currentBalance = customer.Balance;
        var availableCredit = creditLimit - currentBalance;
        var orderAmount = order.TotalAmount;
        
        // Check if split is needed
        if (orderAmount <= availableCredit)
        {
            _logger.LogInformation("Order {SalesId} is within credit limit, no split needed", input.SalesId);
            return new SplitOrderByCreditOutput
            {
                WasSplit = false,
                OriginalSalesId = input.SalesId,
                OriginalOrderAmount = orderAmount,
                CreditLimit = creditLimit,
                CurrentBalance = currentBalance,
                AvailableCredit = availableCredit,
                Message = "Order is within credit limit, no split required"
            };
        }
        
        _logger.LogInformation("Order {SalesId} exceeds credit by {Amount}, splitting", 
            input.SalesId, orderAmount - availableCredit);
        
        // Perform split
        var splitRequest = new SplitOrderRequest
        {
            SalesId = input.SalesId,
            CreditLimit = creditLimit,
            CurrentBalance = currentBalance
        };
        
        var result = await _wcfClient.SplitOrderByCreditAsync(splitRequest, cancellationToken);
        
        _logger.LogInformation("Order {SalesId} split into {NewSalesId}", input.SalesId, result.NewSalesId);
        
        return new SplitOrderByCreditOutput
        {
            WasSplit = result.WasSplit,
            OriginalSalesId = input.SalesId,
            NewSalesId = result.NewSalesId,
            OriginalOrderAmount = result.OriginalOrderAmount,
            AmountWithinLimit = availableCredit,
            AmountExceedingLimit = result.SplitAmount,
            CreditLimit = creditLimit,
            CurrentBalance = currentBalance,
            AvailableCredit = availableCredit,
            SplitLines = result.SplitLines.Select(l => new SplitLineOutput
            {
                OriginalLineNum = l.OriginalLineNum,
                NewLineNum = l.NewLineNum,
                ItemId = l.ItemId,
                OriginalQty = l.OriginalQty,
                RemainingQty = l.RemainingQty,
                SplitQty = l.SplitQty
            }).ToList(),
            Message = $"Order split successfully. {result.NewSalesId} requires approval."
        };
    }
}
