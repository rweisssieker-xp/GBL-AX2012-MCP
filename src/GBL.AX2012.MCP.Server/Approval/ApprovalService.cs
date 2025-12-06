using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using GBL.AX2012.MCP.Core.Options;

namespace GBL.AX2012.MCP.Server.Approval;

public interface IApprovalService
{
    Task<ApprovalResult> RequestApprovalAsync(ApprovalRequest request, CancellationToken cancellationToken = default);
    Task<ApprovalStatus> GetStatusAsync(string approvalId, CancellationToken cancellationToken = default);
    Task<bool> ApproveAsync(string approvalId, string approverId, string? comment = null, CancellationToken cancellationToken = default);
    Task<bool> RejectAsync(string approvalId, string approverId, string reason, CancellationToken cancellationToken = default);
}

public class ApprovalRequest
{
    public string Type { get; set; } = "";
    public string RequesterId { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal? Amount { get; set; }
    public string? Currency { get; set; }
    public Dictionary<string, object> Context { get; set; } = new();
    public TimeSpan? Timeout { get; set; }
}

public class ApprovalResult
{
    public string ApprovalId { get; set; } = "";
    public bool RequiresApproval { get; set; }
    public bool AutoApproved { get; set; }
    public string? Reason { get; set; }
    public string[] Approvers { get; set; } = [];
}

public class ApprovalStatus
{
    public string ApprovalId { get; set; } = "";
    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected, Expired
    public string? ApproverId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? Comment { get; set; }
}

public class ApprovalService : IApprovalService
{
    private readonly ILogger<ApprovalService> _logger;
    private readonly SecurityOptions _securityOptions;
    private readonly Dictionary<string, PendingApproval> _pendingApprovals = new();
    
    public ApprovalService(
        ILogger<ApprovalService> logger,
        IOptions<SecurityOptions> securityOptions)
    {
        _logger = logger;
        _securityOptions = securityOptions.Value;
    }
    
    public Task<ApprovalResult> RequestApprovalAsync(ApprovalRequest request, CancellationToken cancellationToken = default)
    {
        var approvalId = Guid.NewGuid().ToString();
        
        // Check if approval is required based on thresholds
        var requiresApproval = request.Amount.HasValue && request.Amount.Value > _securityOptions.ApprovalThreshold;
        
        if (!requiresApproval)
        {
            _logger.LogDebug("Auto-approved request {Type} for {Requester} - below threshold", 
                request.Type, request.RequesterId);
            
            return Task.FromResult(new ApprovalResult
            {
                ApprovalId = approvalId,
                RequiresApproval = false,
                AutoApproved = true,
                Reason = $"Amount {request.Amount} {request.Currency} below threshold {_securityOptions.ApprovalThreshold}"
            });
        }
        
        // Store pending approval
        var pending = new PendingApproval
        {
            Id = approvalId,
            Request = request,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow + (request.Timeout ?? TimeSpan.FromHours(24))
        };
        
        _pendingApprovals[approvalId] = pending;
        
        _logger.LogInformation("Approval required for {Type}, amount {Amount} {Currency}, id {ApprovalId}", 
            request.Type, request.Amount, request.Currency, approvalId);
        
        return Task.FromResult(new ApprovalResult
        {
            ApprovalId = approvalId,
            RequiresApproval = true,
            AutoApproved = false,
            Reason = $"Amount {request.Amount} {request.Currency} exceeds threshold {_securityOptions.ApprovalThreshold}",
            Approvers = new[] { "MCP_Admin" }
        });
    }
    
    public Task<ApprovalStatus> GetStatusAsync(string approvalId, CancellationToken cancellationToken = default)
    {
        if (!_pendingApprovals.TryGetValue(approvalId, out var pending))
        {
            return Task.FromResult(new ApprovalStatus
            {
                ApprovalId = approvalId,
                Status = "NotFound"
            });
        }
        
        if (pending.ExpiresAt < DateTime.UtcNow)
        {
            return Task.FromResult(new ApprovalStatus
            {
                ApprovalId = approvalId,
                Status = "Expired"
            });
        }
        
        return Task.FromResult(new ApprovalStatus
        {
            ApprovalId = approvalId,
            Status = pending.Status,
            ApproverId = pending.ApproverId,
            ApprovedAt = pending.ApprovedAt,
            Comment = pending.Comment
        });
    }
    
    public Task<bool> ApproveAsync(string approvalId, string approverId, string? comment = null, CancellationToken cancellationToken = default)
    {
        if (!_pendingApprovals.TryGetValue(approvalId, out var pending))
        {
            return Task.FromResult(false);
        }
        
        pending.Status = "Approved";
        pending.ApproverId = approverId;
        pending.ApprovedAt = DateTime.UtcNow;
        pending.Comment = comment;
        
        _logger.LogInformation("Approval {ApprovalId} approved by {Approver}", approvalId, approverId);
        
        return Task.FromResult(true);
    }
    
    public Task<bool> RejectAsync(string approvalId, string approverId, string reason, CancellationToken cancellationToken = default)
    {
        if (!_pendingApprovals.TryGetValue(approvalId, out var pending))
        {
            return Task.FromResult(false);
        }
        
        pending.Status = "Rejected";
        pending.ApproverId = approverId;
        pending.ApprovedAt = DateTime.UtcNow;
        pending.Comment = reason;
        
        _logger.LogInformation("Approval {ApprovalId} rejected by {Approver}: {Reason}", approvalId, approverId, reason);
        
        return Task.FromResult(true);
    }
    
    private class PendingApproval
    {
        public string Id { get; set; } = "";
        public ApprovalRequest Request { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string Status { get; set; } = "Pending";
        public string? ApproverId { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? Comment { get; set; }
    }
}
