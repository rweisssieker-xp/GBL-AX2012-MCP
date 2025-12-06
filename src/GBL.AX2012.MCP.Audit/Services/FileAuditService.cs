using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using GBL.AX2012.MCP.Core.Interfaces;
using GBL.AX2012.MCP.Core.Models;
using GBL.AX2012.MCP.Core.Options;

namespace GBL.AX2012.MCP.Audit.Services;

public class FileAuditService : IAuditService
{
    private readonly AuditOptions _options;
    private readonly ILogger<FileAuditService> _logger;
    private readonly object _lock = new();
    
    public FileAuditService(IOptions<AuditOptions> options, ILogger<FileAuditService> logger)
    {
        _options = options.Value;
        _logger = logger;
        
        // Ensure directory exists
        if (!string.IsNullOrEmpty(_options.FileLogPath))
        {
            Directory.CreateDirectory(_options.FileLogPath);
        }
    }
    
    public Task LogAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        try
        {
            var logPath = _options.FileLogPath;
            if (string.IsNullOrEmpty(logPath))
            {
                logPath = Path.Combine(AppContext.BaseDirectory, "logs", "audit");
                Directory.CreateDirectory(logPath);
            }
            
            var fileName = $"mcp-audit-{entry.Timestamp:yyyy-MM-dd}.jsonl";
            var filePath = Path.Combine(logPath, fileName);
            
            var json = JsonSerializer.Serialize(new
            {
                timestamp = entry.Timestamp.ToString("O"),
                id = entry.Id,
                user_id = entry.UserId,
                tool = entry.ToolName,
                correlation_id = entry.CorrelationId,
                success = entry.Success,
                duration_ms = entry.DurationMs,
                error = entry.Error
            });
            
            lock (_lock)
            {
                File.AppendAllText(filePath, json + Environment.NewLine);
            }
            
            _logger.LogDebug("Audit entry logged to {File}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write audit entry to file");
        }
        
        return Task.CompletedTask;
    }
    
    public Task<IEnumerable<AuditEntry>> QueryAsync(AuditQuery query, CancellationToken cancellationToken = default)
    {
        // File audit doesn't support querying - return empty
        _logger.LogWarning("File audit service does not support querying");
        return Task.FromResult<IEnumerable<AuditEntry>>(Array.Empty<AuditEntry>());
    }
}
