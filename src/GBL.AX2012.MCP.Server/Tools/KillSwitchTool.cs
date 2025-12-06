using FluentValidation;
using Microsoft.Extensions.Logging;
using GBL.AX2012.MCP.Core.Interfaces;
using GBL.AX2012.MCP.Core.Models;
using GBL.AX2012.MCP.Core.Exceptions;

namespace GBL.AX2012.MCP.Server.Tools;

public class KillSwitchInput
{
    public string Action { get; set; } = ""; // activate, deactivate, status
    public string? Reason { get; set; }
    public int? DurationMinutes { get; set; }
    public string[]? AffectedTools { get; set; } // null = all tools
}

public class KillSwitchOutput
{
    public bool KillSwitchActive { get; set; }
    public string Status { get; set; } = "";
    public string? Reason { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? ActivatedBy { get; set; }
    public string[]? AffectedTools { get; set; }
    public int BlockedRequestsCount { get; set; }
}

public class KillSwitchInputValidator : AbstractValidator<KillSwitchInput>
{
    public KillSwitchInputValidator()
    {
        RuleFor(x => x.Action)
            .NotEmpty()
            .Must(x => new[] { "activate", "deactivate", "status" }.Contains(x.ToLower()))
            .WithMessage("action must be 'activate', 'deactivate', or 'status'");
        
        When(x => x.Action.ToLower() == "activate", () =>
        {
            RuleFor(x => x.Reason).NotEmpty().WithMessage("reason is required when activating kill switch");
        });
    }
}

public interface IKillSwitchService
{
    bool IsActive { get; }
    bool IsToolBlocked(string toolName);
    KillSwitchState GetState();
    void Activate(string reason, string userId, int? durationMinutes = null, string[]? affectedTools = null);
    void Deactivate(string userId);
    void IncrementBlockedCount();
}

public class KillSwitchState
{
    public bool Active { get; set; }
    public string? Reason { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? ActivatedBy { get; set; }
    public string[]? AffectedTools { get; set; }
    public int BlockedRequestsCount { get; set; }
}

public class KillSwitchService : IKillSwitchService
{
    private readonly ILogger<KillSwitchService> _logger;
    private readonly INotificationService _notificationService;
    private KillSwitchState _state = new();
    private readonly object _lock = new();
    
    public KillSwitchService(ILogger<KillSwitchService> logger, INotificationService notificationService)
    {
        _logger = logger;
        _notificationService = notificationService;
    }
    
    public bool IsActive
    {
        get
        {
            lock (_lock)
            {
                if (_state.Active && _state.ExpiresAt.HasValue && DateTime.UtcNow > _state.ExpiresAt)
                {
                    _state.Active = false;
                    _logger.LogInformation("Kill switch expired automatically");
                }
                return _state.Active;
            }
        }
    }
    
    public bool IsToolBlocked(string toolName)
    {
        lock (_lock)
        {
            if (!IsActive) return false;
            if (_state.AffectedTools == null) return true; // All tools blocked
            return _state.AffectedTools.Contains(toolName, StringComparer.OrdinalIgnoreCase);
        }
    }
    
    public KillSwitchState GetState()
    {
        lock (_lock)
        {
            return new KillSwitchState
            {
                Active = IsActive,
                Reason = _state.Reason,
                ActivatedAt = _state.ActivatedAt,
                ExpiresAt = _state.ExpiresAt,
                ActivatedBy = _state.ActivatedBy,
                AffectedTools = _state.AffectedTools,
                BlockedRequestsCount = _state.BlockedRequestsCount
            };
        }
    }
    
    public void Activate(string reason, string userId, int? durationMinutes = null, string[]? affectedTools = null)
    {
        lock (_lock)
        {
            _state = new KillSwitchState
            {
                Active = true,
                Reason = reason,
                ActivatedAt = DateTime.UtcNow,
                ExpiresAt = durationMinutes.HasValue ? DateTime.UtcNow.AddMinutes(durationMinutes.Value) : null,
                ActivatedBy = userId,
                AffectedTools = affectedTools,
                BlockedRequestsCount = 0
            };
        }
        
        _logger.LogCritical("KILL SWITCH ACTIVATED by {User}: {Reason}", userId, reason);
        
        _ = _notificationService.SendAlertAsync(
            "KILL SWITCH ACTIVATED",
            $"User: {userId}\nReason: {reason}\nAffected: {(affectedTools == null ? "ALL TOOLS" : string.Join(", ", affectedTools))}",
            NotificationSeverity.Critical);
    }
    
    public void Deactivate(string userId)
    {
        lock (_lock)
        {
            _state.Active = false;
        }
        
        _logger.LogWarning("Kill switch deactivated by {User}", userId);
        
        _ = _notificationService.SendAlertAsync(
            "Kill Switch Deactivated",
            $"Deactivated by: {userId}",
            NotificationSeverity.Warning);
    }
    
    public void IncrementBlockedCount()
    {
        lock (_lock)
        {
            _state.BlockedRequestsCount++;
        }
    }
}

public class KillSwitchTool : ToolBase<KillSwitchInput, KillSwitchOutput>
{
    private readonly IKillSwitchService _killSwitch;
    
    public override string Name => "ax_kill_switch";
    public override string Description => "Emergency stop for all or specific tools (Admin only)";
    
    public KillSwitchTool(
        ILogger<KillSwitchTool> logger,
        IAuditService audit,
        KillSwitchInputValidator validator,
        IKillSwitchService killSwitch)
        : base(logger, audit, validator)
    {
        _killSwitch = killSwitch;
    }
    
    protected override Task<KillSwitchOutput> ExecuteCoreAsync(
        KillSwitchInput input, 
        ToolContext context, 
        CancellationToken cancellationToken)
    {
        // Check admin role
        if (!context.HasRole("MCP_Admin"))
        {
            throw new ForbiddenException("Kill switch requires MCP_Admin role");
        }
        
        switch (input.Action.ToLower())
        {
            case "activate":
                _killSwitch.Activate(input.Reason!, context.UserId, input.DurationMinutes, input.AffectedTools);
                break;
            case "deactivate":
                _killSwitch.Deactivate(context.UserId);
                break;
        }
        
        var state = _killSwitch.GetState();
        
        return Task.FromResult(new KillSwitchOutput
        {
            KillSwitchActive = state.Active,
            Status = state.Active ? "ACTIVE" : "INACTIVE",
            Reason = state.Reason,
            ActivatedAt = state.ActivatedAt,
            ExpiresAt = state.ExpiresAt,
            ActivatedBy = state.ActivatedBy,
            AffectedTools = state.AffectedTools,
            BlockedRequestsCount = state.BlockedRequestsCount
        });
    }
}
