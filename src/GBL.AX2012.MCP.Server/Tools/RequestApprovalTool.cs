using FluentValidation;
using Microsoft.Extensions.Logging;
using GBL.AX2012.MCP.Core.Interfaces;
using GBL.AX2012.MCP.Core.Models;
using GBL.AX2012.MCP.Server.Approval;

namespace GBL.AX2012.MCP.Server.Tools;

public class RequestApprovalInput
{
    public string Type { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal? Amount { get; set; }
    public string? Currency { get; set; }
    public string? Reference { get; set; }
}

public class RequestApprovalOutput
{
    public string ApprovalId { get; set; } = "";
    public bool RequiresApproval { get; set; }
    public bool AutoApproved { get; set; }
    public string Status { get; set; } = "";
    public string? Reason { get; set; }
    public string[] Approvers { get; set; } = [];
    public string? CheckStatusUrl { get; set; }
}

public class RequestApprovalInputValidator : AbstractValidator<RequestApprovalInput>
{
    public RequestApprovalInputValidator()
    {
        RuleFor(x => x.Type).NotEmpty().WithMessage("type is required");
        RuleFor(x => x.Description).NotEmpty().WithMessage("description is required");
    }
}

public class RequestApprovalTool : ToolBase<RequestApprovalInput, RequestApprovalOutput>
{
    private readonly IApprovalService _approvalService;
    
    public override string Name => "ax_request_approval";
    public override string Description => "Request approval for a high-value operation";
    
    public RequestApprovalTool(
        ILogger<RequestApprovalTool> logger,
        IAuditService audit,
        RequestApprovalInputValidator validator,
        IApprovalService approvalService)
        : base(logger, audit, validator)
    {
        _approvalService = approvalService;
    }
    
    protected override async Task<RequestApprovalOutput> ExecuteCoreAsync(
        RequestApprovalInput input, 
        ToolContext context, 
        CancellationToken cancellationToken)
    {
        var request = new ApprovalRequest
        {
            Type = input.Type,
            RequesterId = context.UserId,
            Description = input.Description,
            Amount = input.Amount,
            Currency = input.Currency ?? "EUR",
            Context = new Dictionary<string, object>
            {
                ["reference"] = input.Reference ?? "",
                ["correlation_id"] = context.CorrelationId
            }
        };
        
        var result = await _approvalService.RequestApprovalAsync(request, cancellationToken);
        
        return new RequestApprovalOutput
        {
            ApprovalId = result.ApprovalId,
            RequiresApproval = result.RequiresApproval,
            AutoApproved = result.AutoApproved,
            Status = result.AutoApproved ? "Approved" : "Pending",
            Reason = result.Reason,
            Approvers = result.Approvers,
            CheckStatusUrl = result.RequiresApproval ? $"/approval/{result.ApprovalId}/status" : null
        };
    }
}
