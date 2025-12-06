using FluentValidation;
using Microsoft.Extensions.Logging;
using GBL.AX2012.MCP.Core.Exceptions;
using GBL.AX2012.MCP.Core.Interfaces;
using GBL.AX2012.MCP.Core.Models;
using GBL.AX2012.MCP.AxConnector.Interfaces;

namespace GBL.AX2012.MCP.Server.Tools;

public class GetCustomerInput
{
    public string? CustomerAccount { get; set; }
    public string? CustomerName { get; set; }
    public bool IncludeAddresses { get; set; } = false;
    public bool IncludeContacts { get; set; } = false;
}

public class GetCustomerOutput
{
    public string CustomerAccount { get; set; } = "";
    public string Name { get; set; } = "";
    public string Currency { get; set; } = "";
    public decimal CreditLimit { get; set; }
    public decimal CreditUsed { get; set; }
    public decimal CreditAvailable => CreditLimit - CreditUsed;
    public string PaymentTerms { get; set; } = "";
    public string PriceGroup { get; set; } = "";
    public bool Blocked { get; set; }
}

public class CustomerSearchOutput
{
    public List<CustomerSearchResult> Matches { get; set; } = new();
}

public class CustomerSearchResult
{
    public string CustomerAccount { get; set; } = "";
    public string Name { get; set; } = "";
    public int Confidence { get; set; }
}

public class GetCustomerInputValidator : AbstractValidator<GetCustomerInput>
{
    public GetCustomerInputValidator()
    {
        RuleFor(x => x)
            .Must(x => !string.IsNullOrEmpty(x.CustomerAccount) || !string.IsNullOrEmpty(x.CustomerName))
            .WithMessage("Either customer_account or customer_name must be provided");
    }
}

public class GetCustomerTool : ToolBase<GetCustomerInput, object>
{
    private readonly IAifClient _aifClient;
    
    public override string Name => "ax_get_customer";
    public override string Description => "Retrieve customer information from AX 2012 by account number or name search";
    
    public GetCustomerTool(
        ILogger<GetCustomerTool> logger,
        IAuditService audit,
        GetCustomerInputValidator validator,
        IAifClient aifClient)
        : base(logger, audit, validator)
    {
        _aifClient = aifClient;
    }
    
    protected override async Task<object> ExecuteCoreAsync(
        GetCustomerInput input, 
        ToolContext context, 
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(input.CustomerAccount))
        {
            var customer = await _aifClient.GetCustomerAsync(input.CustomerAccount, cancellationToken);
            
            if (customer == null)
            {
                throw new AxException("CUST_NOT_FOUND", $"Customer {input.CustomerAccount} not found");
            }
            
            return new GetCustomerOutput
            {
                CustomerAccount = customer.AccountNum,
                Name = customer.Name,
                Currency = customer.Currency,
                CreditLimit = customer.CreditLimit,
                CreditUsed = customer.CreditUsed,
                PaymentTerms = customer.PaymentTerms,
                PriceGroup = customer.PriceGroup,
                Blocked = customer.Blocked
            };
        }
        else if (!string.IsNullOrEmpty(input.CustomerName))
        {
            var customers = await _aifClient.SearchCustomersAsync(input.CustomerName, 5, cancellationToken);
            
            return new CustomerSearchOutput
            {
                Matches = customers.Select(c => new CustomerSearchResult
                {
                    CustomerAccount = c.AccountNum,
                    Name = c.Name,
                    Confidence = c.MatchConfidence
                }).ToList()
            };
        }
        
        throw new AxException("INVALID_INPUT", "Either customer_account or customer_name must be provided");
    }
}
