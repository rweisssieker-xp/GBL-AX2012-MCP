using FluentValidation;
using Microsoft.Extensions.Logging;
using GBL.AX2012.MCP.Core.Interfaces;
using GBL.AX2012.MCP.Core.Models;
using GBL.AX2012.MCP.Server.Approval;

namespace GBL.AX2012.MCP.Server.Tools;

public class GetApprovalStatusInput
{
    public string ApprovalId { get; set; } = "";
}

public class GetApprovalStatusOutput
{
    public string ApprovalId { get; set; } = "";
    public string Status { get; set; } = "";
    public string? ApproverId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? Comment { get; set; }
    public bool CanProceed { get; set; }
}

public class GetApprovalStatusInputValidator : AbstractValidator<GetApprovalStatusInput>
{
    public GetApprovalStatusInputValidator()
    {
        RuleFor(x => x.ApprovalId).NotEmpty().WithMessage("approval_id is required");
    }
}

public class GetApprovalStatusTool : ToolBase<GetApprovalStatusInput, GetApprovalStatusOutput>
{
    private readonly IApprovalService _approvalService;
    
    public override string Name => "ax_get_approval_status";
    public override string Description => "Check the status of an approval request";
    
    public GetApprovalStatusTool(
        ILogger<GetApprovalStatusTool> logger,
        IAuditService audit,
        GetApprovalStatusInputValidator validator,
        IApprovalService approvalService)
        : base(logger, audit, validator)
    {
        _approvalService = approvalService;
    }
    
    protected override async Task<GetApprovalStatusOutput> ExecuteCoreAsync(
        GetApprovalStatusInput input, 
        ToolContext context, 
        CancellationToken cancellationToken)
    {
        var status = await _approvalService.GetStatusAsync(input.ApprovalId, cancellationToken);
        
        return new GetApprovalStatusOutput
        {
            ApprovalId = status.ApprovalId,
            Status = status.Status,
            ApproverId = status.ApproverId,
            ApprovedAt = status.ApprovedAt,
            Comment = status.Comment,
            CanProceed = status.Status == "Approved"
        };
    }
}
