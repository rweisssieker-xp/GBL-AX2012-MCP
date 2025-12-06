using FluentValidation;
using Microsoft.Extensions.Logging;
using GBL.AX2012.MCP.Core.Exceptions;
using GBL.AX2012.MCP.Core.Interfaces;
using GBL.AX2012.MCP.Core.Models;
using GBL.AX2012.MCP.AxConnector.Interfaces;

namespace GBL.AX2012.MCP.Server.Tools;

public class GetCustomerAgingInput
{
    public string CustomerAccount { get; set; } = "";
    public DateTime? AsOfDate { get; set; }
    public bool IncludeOpenInvoices { get; set; } = true;
}

public class GetCustomerAgingOutput
{
    public string CustomerAccount { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public DateTime AsOfDate { get; set; }
    public string Currency { get; set; } = "";
    public decimal TotalBalance { get; set; }
    public AgingBuckets Aging { get; set; } = new();
    public int DunningLevel { get; set; }
    public DateTime? LastPaymentDate { get; set; }
    public List<OpenInvoice>? OpenInvoices { get; set; }
}

public class AgingBuckets
{
    public decimal Current { get; set; }
    public decimal Days1To30 { get; set; }
    public decimal Days31To60 { get; set; }
    public decimal Days61To90 { get; set; }
    public decimal Over90Days { get; set; }
}

public class OpenInvoice
{
    public string InvoiceId { get; set; } = "";
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public decimal Amount { get; set; }
    public decimal OpenAmount { get; set; }
    public int DaysOverdue { get; set; }
}

public class GetCustomerAgingInputValidator : AbstractValidator<GetCustomerAgingInput>
{
    public GetCustomerAgingInputValidator()
    {
        RuleFor(x => x.CustomerAccount).NotEmpty().WithMessage("customer_account is required");
    }
}

public class GetCustomerAgingTool : ToolBase<GetCustomerAgingInput, GetCustomerAgingOutput>
{
    private readonly IAifClient _aifClient;
    
    public override string Name => "ax_get_customer_aging";
    public override string Description => "Get customer aging/AR balance and open invoices for dunning";
    
    public GetCustomerAgingTool(
        ILogger<GetCustomerAgingTool> logger,
        IAuditService audit,
        GetCustomerAgingInputValidator validator,
        IAifClient aifClient)
        : base(logger, audit, validator)
    {
        _aifClient = aifClient;
    }
    
    protected override async Task<GetCustomerAgingOutput> ExecuteCoreAsync(
        GetCustomerAgingInput input, 
        ToolContext context, 
        CancellationToken cancellationToken)
    {
        var customer = await _aifClient.GetCustomerAsync(input.CustomerAccount, cancellationToken);
        if (customer == null)
        {
            throw new AxException("CUST_NOT_FOUND", $"Customer {input.CustomerAccount} not found");
        }
        
        var asOfDate = input.AsOfDate ?? DateTime.Today;
        
        // Simulated aging data - in real impl would query CustTrans
        var aging = new AgingBuckets
        {
            Current = customer.CreditUsed * 0.4m,
            Days1To30 = customer.CreditUsed * 0.3m,
            Days31To60 = customer.CreditUsed * 0.15m,
            Days61To90 = customer.CreditUsed * 0.1m,
            Over90Days = customer.CreditUsed * 0.05m
        };
        
        var dunningLevel = aging.Over90Days > 0 ? 3 :
                          aging.Days61To90 > 0 ? 2 :
                          aging.Days31To60 > 0 ? 1 : 0;
        
        var output = new GetCustomerAgingOutput
        {
            CustomerAccount = customer.AccountNum,
            CustomerName = customer.Name,
            AsOfDate = asOfDate,
            Currency = customer.Currency,
            TotalBalance = customer.CreditUsed,
            Aging = aging,
            DunningLevel = dunningLevel,
            LastPaymentDate = DateTime.Today.AddDays(-15)
        };
        
        if (input.IncludeOpenInvoices)
        {
            output.OpenInvoices = new List<OpenInvoice>
            {
                new OpenInvoice
                {
                    InvoiceId = "INV-2024-001",
                    InvoiceDate = DateTime.Today.AddDays(-45),
                    DueDate = DateTime.Today.AddDays(-15),
                    Amount = aging.Days31To60,
                    OpenAmount = aging.Days31To60,
                    DaysOverdue = 15
                }
            };
        }
        
        return output;
    }
}
