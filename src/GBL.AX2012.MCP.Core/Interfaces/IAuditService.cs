using GBL.AX2012.MCP.Core.Models;

namespace GBL.AX2012.MCP.Core.Interfaces;

public interface IAuditService
{
    Task LogAsync(AuditEntry entry, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditEntry>> QueryAsync(AuditQuery query, CancellationToken cancellationToken = default);
}
