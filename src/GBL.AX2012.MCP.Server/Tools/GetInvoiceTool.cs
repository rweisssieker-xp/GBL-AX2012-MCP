using FluentValidation;
using Microsoft.Extensions.Logging;
using GBL.AX2012.MCP.Core.Exceptions;
using GBL.AX2012.MCP.Core.Interfaces;
using GBL.AX2012.MCP.Core.Models;
using GBL.AX2012.MCP.AxConnector.Interfaces;

namespace GBL.AX2012.MCP.Server.Tools;

public class GetInvoiceInput
{
    public string? InvoiceId { get; set; }
    public string? SalesId { get; set; }
    public string? CustomerAccount { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public bool IncludeLines { get; set; } = true;
}

public class GetInvoiceOutput
{
    public string InvoiceId { get; set; } = "";
    public string SalesId { get; set; } = "";
    public string CustomerAccount { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public decimal NetAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal OpenAmount { get; set; }
    public string Currency { get; set; } = "";
    public string Status { get; set; } = "";
    public List<InvoiceLineOutput>? Lines { get; set; }
}

public class InvoiceLineOutput
{
    public int LineNum { get; set; }
    public string ItemId { get; set; } = "";
    public string ItemName { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineAmount { get; set; }
    public decimal TaxAmount { get; set; }
}

public class InvoiceListOutput
{
    public List<InvoiceSummary> Invoices { get; set; } = new();
    public int TotalCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalOpen { get; set; }
}

public class InvoiceSummary
{
    public string InvoiceId { get; set; } = "";
    public string SalesId { get; set; } = "";
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal OpenAmount { get; set; }
    public string Status { get; set; } = "";
}

public class GetInvoiceInputValidator : AbstractValidator<GetInvoiceInput>
{
    public GetInvoiceInputValidator()
    {
        RuleFor(x => x)
            .Must(x => !string.IsNullOrEmpty(x.InvoiceId) || !string.IsNullOrEmpty(x.SalesId) || !string.IsNullOrEmpty(x.CustomerAccount))
            .WithMessage("Either invoice_id, sales_id, or customer_account is required");
    }
}

public class GetInvoiceTool : ToolBase<GetInvoiceInput, object>
{
    private readonly IAifClient _aifClient;
    
    public override string Name => "ax_get_invoice";
    public override string Description => "Get invoice by ID, sales order, or customer";
    
    public GetInvoiceTool(
        ILogger<GetInvoiceTool> logger,
        IAuditService audit,
        GetInvoiceInputValidator validator,
        IAifClient aifClient)
        : base(logger, audit, validator)
    {
        _aifClient = aifClient;
    }
    
    protected override async Task<object> ExecuteCoreAsync(
        GetInvoiceInput input, 
        ToolContext context, 
        CancellationToken cancellationToken)
    {
        // By invoice ID - return single invoice
        if (!string.IsNullOrEmpty(input.InvoiceId))
        {
            // Simulated - would call AIF
            var invoice = new GetInvoiceOutput
            {
                InvoiceId = input.InvoiceId,
                SalesId = "SO-2024-001",
                CustomerAccount = "CUST-001",
                CustomerName = "Test Customer",
                InvoiceDate = DateTime.Today.AddDays(-30),
                DueDate = DateTime.Today,
                NetAmount = 10000m,
                TaxAmount = 1900m,
                GrossAmount = 11900m,
                PaidAmount = 0m,
                OpenAmount = 11900m,
                Currency = "EUR",
                Status = "Open"
            };
            
            if (input.IncludeLines)
            {
                invoice.Lines = new List<InvoiceLineOutput>
                {
                    new InvoiceLineOutput
                    {
                        LineNum = 1,
                        ItemId = "ITEM-100",
                        ItemName = "Widget Pro",
                        Quantity = 50,
                        UnitPrice = 200m,
                        LineAmount = 10000m,
                        TaxAmount = 1900m
                    }
                };
            }
            
            return invoice;
        }
        
        // By sales order or customer - return list
        var invoices = new List<InvoiceSummary>
        {
            new InvoiceSummary
            {
                InvoiceId = "INV-2024-001",
                SalesId = input.SalesId ?? "SO-2024-001",
                InvoiceDate = DateTime.Today.AddDays(-30),
                DueDate = DateTime.Today,
                GrossAmount = 11900m,
                OpenAmount = 11900m,
                Status = "Open"
            }
        };
        
        return new InvoiceListOutput
        {
            Invoices = invoices,
            TotalCount = invoices.Count,
            TotalAmount = invoices.Sum(i => i.GrossAmount),
            TotalOpen = invoices.Sum(i => i.OpenAmount)
        };
    }
}
