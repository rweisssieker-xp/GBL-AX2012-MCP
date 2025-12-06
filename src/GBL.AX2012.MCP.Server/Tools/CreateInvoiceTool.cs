using FluentValidation;
using Microsoft.Extensions.Logging;
using GBL.AX2012.MCP.Core.Exceptions;
using GBL.AX2012.MCP.Core.Interfaces;
using GBL.AX2012.MCP.Core.Models;
using GBL.AX2012.MCP.AxConnector.Interfaces;

namespace GBL.AX2012.MCP.Server.Tools;

public class CreateInvoiceInput
{
    public string SalesId { get; set; } = "";
    public DateTime? InvoiceDate { get; set; }
    public string? InvoiceAccount { get; set; }
    public bool PostImmediately { get; set; } = true;
}

public class CreateInvoiceOutput
{
    public string SalesId { get; set; } = "";
    public string InvoiceId { get; set; } = "";
    public DateTime InvoiceDate { get; set; }
    public string CustomerAccount { get; set; } = "";
    public string InvoiceAccount { get; set; } = "";
    public decimal NetAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal GrossAmount { get; set; }
    public string Currency { get; set; } = "";
    public DateTime DueDate { get; set; }
    public string Status { get; set; } = "";
}

public class CreateInvoiceInputValidator : AbstractValidator<CreateInvoiceInput>
{
    public CreateInvoiceInputValidator()
    {
        RuleFor(x => x.SalesId).NotEmpty().WithMessage("sales_id is required");
    }
}

public class CreateInvoiceTool : ToolBase<CreateInvoiceInput, CreateInvoiceOutput>
{
    private readonly IWcfClient _wcfClient;
    private readonly IAifClient _aifClient;
    
    public override string Name => "ax_create_invoice";
    public override string Description => "Create and optionally post an invoice for a sales order";
    
    public CreateInvoiceTool(
        ILogger<CreateInvoiceTool> logger,
        IAuditService audit,
        CreateInvoiceInputValidator validator,
        IWcfClient wcfClient,
        IAifClient aifClient)
        : base(logger, audit, validator)
    {
        _wcfClient = wcfClient;
        _aifClient = aifClient;
    }
    
    protected override async Task<CreateInvoiceOutput> ExecuteCoreAsync(
        CreateInvoiceInput input, 
        ToolContext context, 
        CancellationToken cancellationToken)
    {
        var order = await _aifClient.GetSalesOrderAsync(input.SalesId, cancellationToken);
        if (order == null)
        {
            throw new AxException("ORDER_NOT_FOUND", $"Sales order {input.SalesId} not found");
        }
        
        // Check if there's anything to invoice (shipped but not invoiced)
        var shippedLines = order.Lines.Where(l => l.DeliveredQty > 0).ToList();
        if (!shippedLines.Any())
        {
            throw new AxException("NOTHING_TO_INVOICE", "No shipped lines to invoice");
        }
        
        var customer = await _aifClient.GetCustomerAsync(order.CustomerAccount, cancellationToken);
        
        var invoiceDate = input.InvoiceDate ?? DateTime.Today;
        var invoiceId = $"INV-{input.SalesId}-{DateTime.Now:yyyyMMddHHmmss}";
        var netAmount = shippedLines.Sum(l => l.LineAmount);
        var taxRate = 0.19m; // Default VAT
        var taxAmount = netAmount * taxRate;
        
        // Calculate due date based on payment terms
        var dueDate = invoiceDate.AddDays(30); // Default Net30
        
        _logger.LogInformation("Creating invoice {InvoiceId} for order {SalesId}, amount {Amount}", 
            invoiceId, input.SalesId, netAmount + taxAmount);
        
        return new CreateInvoiceOutput
        {
            SalesId = input.SalesId,
            InvoiceId = invoiceId,
            InvoiceDate = invoiceDate,
            CustomerAccount = order.CustomerAccount,
            InvoiceAccount = input.InvoiceAccount ?? order.CustomerAccount,
            NetAmount = netAmount,
            TaxAmount = taxAmount,
            GrossAmount = netAmount + taxAmount,
            Currency = order.Currency,
            DueDate = dueDate,
            Status = input.PostImmediately ? "Posted" : "Draft"
        };
    }
}
