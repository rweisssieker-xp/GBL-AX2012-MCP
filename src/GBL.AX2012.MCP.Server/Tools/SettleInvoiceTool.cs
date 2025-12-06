using FluentValidation;
using Microsoft.Extensions.Logging;
using GBL.AX2012.MCP.Core.Exceptions;
using GBL.AX2012.MCP.Core.Interfaces;
using GBL.AX2012.MCP.Core.Models;
using GBL.AX2012.MCP.AxConnector.Interfaces;

namespace GBL.AX2012.MCP.Server.Tools;

public class SettleInvoiceInput
{
    public string InvoiceId { get; set; } = "";
    public string PaymentId { get; set; } = "";
    public decimal? Amount { get; set; }
}

public class SettleInvoiceOutput
{
    public string InvoiceId { get; set; } = "";
    public string PaymentId { get; set; } = "";
    public decimal SettledAmount { get; set; }
    public decimal InvoiceOpenBefore { get; set; }
    public decimal InvoiceOpenAfter { get; set; }
    public decimal PaymentOpenBefore { get; set; }
    public decimal PaymentOpenAfter { get; set; }
    public bool InvoiceFullySettled { get; set; }
    public DateTime SettlementDate { get; set; }
}

public class SettleInvoiceInputValidator : AbstractValidator<SettleInvoiceInput>
{
    public SettleInvoiceInputValidator()
    {
        RuleFor(x => x.InvoiceId).NotEmpty().WithMessage("invoice_id is required");
        RuleFor(x => x.PaymentId).NotEmpty().WithMessage("payment_id is required");
    }
}

public class SettleInvoiceTool : ToolBase<SettleInvoiceInput, SettleInvoiceOutput>
{
    private readonly IWcfClient _wcfClient;
    
    public override string Name => "ax_settle_invoice";
    public override string Description => "Settle an invoice against a payment";
    
    public SettleInvoiceTool(
        ILogger<SettleInvoiceTool> logger,
        IAuditService audit,
        SettleInvoiceInputValidator validator,
        IWcfClient wcfClient)
        : base(logger, audit, validator)
    {
        _wcfClient = wcfClient;
    }
    
    protected override async Task<SettleInvoiceOutput> ExecuteCoreAsync(
        SettleInvoiceInput input, 
        ToolContext context, 
        CancellationToken cancellationToken)
    {
        // Simulated settlement - would query actual open amounts
        var invoiceOpen = 1000m;
        var paymentOpen = input.Amount ?? invoiceOpen;
        var settleAmount = Math.Min(invoiceOpen, paymentOpen);
        
        _logger.LogInformation("Settling invoice {Invoice} against payment {Payment}, amount {Amount}", 
            input.InvoiceId, input.PaymentId, settleAmount);
        
        return new SettleInvoiceOutput
        {
            InvoiceId = input.InvoiceId,
            PaymentId = input.PaymentId,
            SettledAmount = settleAmount,
            InvoiceOpenBefore = invoiceOpen,
            InvoiceOpenAfter = invoiceOpen - settleAmount,
            PaymentOpenBefore = paymentOpen,
            PaymentOpenAfter = paymentOpen - settleAmount,
            InvoiceFullySettled = settleAmount >= invoiceOpen,
            SettlementDate = DateTime.Today
        };
    }
}
