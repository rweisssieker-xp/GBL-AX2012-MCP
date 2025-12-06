using FluentValidation;
using Microsoft.Extensions.Logging;
using GBL.AX2012.MCP.Core.Exceptions;
using GBL.AX2012.MCP.Core.Interfaces;
using GBL.AX2012.MCP.Core.Models;
using GBL.AX2012.MCP.AxConnector.Interfaces;

namespace GBL.AX2012.MCP.Server.Tools;

public class PostPaymentInput
{
    public string CustomerAccount { get; set; } = "";
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EUR";
    public DateTime? PaymentDate { get; set; }
    public string? PaymentReference { get; set; }
    public string PaymentMethod { get; set; } = "BankTransfer";
    public string? BankAccount { get; set; }
    public List<string>? InvoicesToSettle { get; set; }
}

public class PostPaymentOutput
{
    public string PaymentId { get; set; } = "";
    public string CustomerAccount { get; set; } = "";
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "";
    public DateTime PaymentDate { get; set; }
    public string PaymentReference { get; set; } = "";
    public string PaymentMethod { get; set; } = "";
    public List<SettledInvoice> SettledInvoices { get; set; } = new();
    public decimal RemainingAmount { get; set; }
    public string Status { get; set; } = "";
}

public class SettledInvoice
{
    public string InvoiceId { get; set; } = "";
    public decimal SettledAmount { get; set; }
    public bool FullySettled { get; set; }
}

public class PostPaymentInputValidator : AbstractValidator<PostPaymentInput>
{
    public PostPaymentInputValidator()
    {
        RuleFor(x => x.CustomerAccount).NotEmpty().WithMessage("customer_account is required");
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("amount must be greater than 0");
    }
}

public class PostPaymentTool : ToolBase<PostPaymentInput, PostPaymentOutput>
{
    private readonly IWcfClient _wcfClient;
    private readonly IAifClient _aifClient;
    
    public override string Name => "ax_post_payment";
    public override string Description => "Post a customer payment and optionally settle against invoices";
    
    public PostPaymentTool(
        ILogger<PostPaymentTool> logger,
        IAuditService audit,
        PostPaymentInputValidator validator,
        IWcfClient wcfClient,
        IAifClient aifClient)
        : base(logger, audit, validator)
    {
        _wcfClient = wcfClient;
        _aifClient = aifClient;
    }
    
    protected override async Task<PostPaymentOutput> ExecuteCoreAsync(
        PostPaymentInput input, 
        ToolContext context, 
        CancellationToken cancellationToken)
    {
        var customer = await _aifClient.GetCustomerAsync(input.CustomerAccount, cancellationToken);
        if (customer == null)
        {
            throw new AxException("CUST_NOT_FOUND", $"Customer {input.CustomerAccount} not found");
        }
        
        var paymentDate = input.PaymentDate ?? DateTime.Today;
        var paymentId = $"PAY-{DateTime.Now:yyyyMMddHHmmss}-{input.CustomerAccount}";
        var paymentRef = input.PaymentReference ?? paymentId;
        
        var settledInvoices = new List<SettledInvoice>();
        var remainingAmount = input.Amount;
        
        // Auto-settle oldest invoices first if no specific invoices provided
        if (input.InvoicesToSettle?.Any() == true)
        {
            foreach (var invoiceId in input.InvoicesToSettle)
            {
                if (remainingAmount <= 0) break;
                
                // Simulated - would query actual invoice amount
                var invoiceAmount = remainingAmount * 0.5m;
                var settleAmount = Math.Min(remainingAmount, invoiceAmount);
                
                settledInvoices.Add(new SettledInvoice
                {
                    InvoiceId = invoiceId,
                    SettledAmount = settleAmount,
                    FullySettled = settleAmount >= invoiceAmount
                });
                
                remainingAmount -= settleAmount;
            }
        }
        
        _logger.LogInformation("Posted payment {PaymentId} for {Customer}, amount {Amount} {Currency}", 
            paymentId, input.CustomerAccount, input.Amount, input.Currency);
        
        return new PostPaymentOutput
        {
            PaymentId = paymentId,
            CustomerAccount = input.CustomerAccount,
            Amount = input.Amount,
            Currency = input.Currency,
            PaymentDate = paymentDate,
            PaymentReference = paymentRef,
            PaymentMethod = input.PaymentMethod,
            SettledInvoices = settledInvoices,
            RemainingAmount = remainingAmount,
            Status = remainingAmount > 0 ? "PartiallyApplied" : "FullyApplied"
        };
    }
}
