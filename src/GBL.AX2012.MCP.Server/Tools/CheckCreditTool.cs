using FluentValidation;
using Microsoft.Extensions.Logging;
using GBL.AX2012.MCP.Core.Exceptions;
using GBL.AX2012.MCP.Core.Interfaces;
using GBL.AX2012.MCP.Core.Models;
using GBL.AX2012.MCP.AxConnector.Interfaces;

namespace GBL.AX2012.MCP.Server.Tools;

public class CheckCreditInput
{
    public string CustomerAccount { get; set; } = "";
    public decimal? ProposedAmount { get; set; }
    public string? Currency { get; set; }
}

public class CheckCreditOutput
{
    public string CustomerAccount { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string Currency { get; set; } = "";
    public decimal CreditLimit { get; set; }
    public decimal CreditUsed { get; set; }
    public decimal CreditAvailable { get; set; }
    public decimal? ProposedAmount { get; set; }
    public bool WouldExceedLimit { get; set; }
    public decimal? AmountOverLimit { get; set; }
    public string CreditStatus { get; set; } = "";
    public string Recommendation { get; set; } = "";
    public bool CustomerBlocked { get; set; }
    public int DunningLevel { get; set; }
}

public class CheckCreditInputValidator : AbstractValidator<CheckCreditInput>
{
    public CheckCreditInputValidator()
    {
        RuleFor(x => x.CustomerAccount).NotEmpty().WithMessage("customer_account is required");
    }
}

public class CheckCreditTool : ToolBase<CheckCreditInput, CheckCreditOutput>
{
    private readonly IAifClient _aifClient;
    
    public override string Name => "ax_check_credit";
    public override string Description => "Check customer credit limit and availability";
    
    public CheckCreditTool(
        ILogger<CheckCreditTool> logger,
        IAuditService audit,
        CheckCreditInputValidator validator,
        IAifClient aifClient)
        : base(logger, audit, validator)
    {
        _aifClient = aifClient;
    }
    
    protected override async Task<CheckCreditOutput> ExecuteCoreAsync(
        CheckCreditInput input, 
        ToolContext context, 
        CancellationToken cancellationToken)
    {
        var customer = await _aifClient.GetCustomerAsync(input.CustomerAccount, cancellationToken);
        if (customer == null)
        {
            throw new AxException("CUST_NOT_FOUND", $"Customer {input.CustomerAccount} not found");
        }
        
        var creditAvailable = customer.CreditLimit - customer.CreditUsed;
        var wouldExceed = input.ProposedAmount.HasValue && input.ProposedAmount.Value > creditAvailable;
        var amountOver = wouldExceed ? input.ProposedAmount!.Value - creditAvailable : (decimal?)null;
        
        // Determine credit status
        var utilizationPct = customer.CreditLimit > 0 ? (customer.CreditUsed / customer.CreditLimit) * 100 : 0;
        var creditStatus = utilizationPct switch
        {
            >= 100 => "EXCEEDED",
            >= 90 => "CRITICAL",
            >= 75 => "WARNING",
            >= 50 => "MODERATE",
            _ => "HEALTHY"
        };
        
        // Generate recommendation
        var recommendation = (customer.Blocked, wouldExceed, creditStatus) switch
        {
            (true, _, _) => "REJECT: Customer is blocked",
            (_, true, _) => $"REJECT: Would exceed credit limit by {amountOver:N2} {customer.Currency}",
            (_, _, "EXCEEDED") => "REJECT: Credit limit already exceeded",
            (_, _, "CRITICAL") => "REVIEW: Credit utilization critical (>90%)",
            (_, _, "WARNING") => "APPROVE_WITH_CAUTION: Credit utilization high (>75%)",
            _ => "APPROVE: Credit check passed"
        };
        
        return new CheckCreditOutput
        {
            CustomerAccount = customer.AccountNum,
            CustomerName = customer.Name,
            Currency = customer.Currency,
            CreditLimit = customer.CreditLimit,
            CreditUsed = customer.CreditUsed,
            CreditAvailable = creditAvailable,
            ProposedAmount = input.ProposedAmount,
            WouldExceedLimit = wouldExceed,
            AmountOverLimit = amountOver,
            CreditStatus = creditStatus,
            Recommendation = recommendation,
            CustomerBlocked = customer.Blocked,
            DunningLevel = 0 // Would come from CustTable
        };
    }
}
