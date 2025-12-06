---
epic: 5
title: "Security & Audit"
stories: 5
status: "READY"
project_name: "GBL-AX2012-MCP"
date: "2025-12-06"
---

# Epic 5: Security & Audit - Implementation Plans

## Story 5.1: Windows Authentication

### Implementation Plan

```
📁 Windows Authentication
│
├── 1. Create authentication middleware
│   └── Extract Windows identity
│
├── 2. Create IAuthenticationService
│   └── Get current user
│
├── 3. Integrate with ToolContext
│   └── Populate UserId
│
└── 4. Unit tests
    └── Test identity extraction
```

### Files to Create

```csharp
// src/GBL.AX2012.MCP.Server/Security/IAuthenticationService.cs
namespace GBL.AX2012.MCP.Server.Security;

public interface IAuthenticationService
{
    Task<AuthenticationResult> AuthenticateAsync(CancellationToken cancellationToken = default);
    string? GetCurrentUserId();
    string[] GetCurrentUserRoles();
}

public class AuthenticationResult
{
    public bool IsAuthenticated { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string[] Roles { get; set; } = [];
    public string? ErrorMessage { get; set; }
}

// src/GBL.AX2012.MCP.Server/Security/WindowsAuthenticationService.cs
namespace GBL.AX2012.MCP.Server.Security;

public class WindowsAuthenticationService : IAuthenticationService
{
    private readonly ILogger<WindowsAuthenticationService> _logger;
    private readonly SecurityOptions _options;
    private AuthenticationResult? _cachedResult;
    
    public WindowsAuthenticationService(
        ILogger<WindowsAuthenticationService> logger,
        IOptions<SecurityOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }
    
    public Task<AuthenticationResult> AuthenticateAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedResult != null)
        {
            return Task.FromResult(_cachedResult);
        }
        
        try
        {
            var identity = WindowsIdentity.GetCurrent();
            
            if (identity == null || !identity.IsAuthenticated)
            {
                _logger.LogWarning("No authenticated Windows identity found");
                return Task.FromResult(new AuthenticationResult
                {
                    IsAuthenticated = false,
                    ErrorMessage = "Not authenticated"
                });
            }
            
            var roles = GetRolesFromGroups(identity);
            
            _cachedResult = new AuthenticationResult
            {
                IsAuthenticated = true,
                UserId = identity.Name,
                UserName = identity.Name.Split('\\').LastOrDefault() ?? identity.Name,
                Roles = roles
            };
            
            _logger.LogDebug("Authenticated user {UserId} with roles {Roles}", 
                _cachedResult.UserId, string.Join(", ", roles));
            
            return Task.FromResult(_cachedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Authentication failed");
            return Task.FromResult(new AuthenticationResult
            {
                IsAuthenticated = false,
                ErrorMessage = ex.Message
            });
        }
    }
    
    public string? GetCurrentUserId()
    {
        return _cachedResult?.UserId ?? WindowsIdentity.GetCurrent()?.Name;
    }
    
    public string[] GetCurrentUserRoles()
    {
        return _cachedResult?.Roles ?? [];
    }
    
    private string[] GetRolesFromGroups(WindowsIdentity identity)
    {
        var roles = new List<string>();
        
        if (identity.Groups == null)
        {
            return roles.ToArray();
        }
        
        foreach (var group in identity.Groups)
        {
            try
            {
                var groupName = group.Translate(typeof(NTAccount))?.Value;
                if (groupName == null) continue;
                
                // Map AD groups to MCP roles
                if (groupName.EndsWith("MCP-Users-Read", StringComparison.OrdinalIgnoreCase))
                {
                    roles.Add("MCP_Read");
                }
                else if (groupName.EndsWith("MCP-Users-Write", StringComparison.OrdinalIgnoreCase))
                {
                    roles.Add("MCP_Write");
                    roles.Add("MCP_Read"); // Write implies Read
                }
                else if (groupName.EndsWith("MCP-Admins", StringComparison.OrdinalIgnoreCase))
                {
                    roles.Add("MCP_Admin");
                    roles.Add("MCP_Write");
                    roles.Add("MCP_Read"); // Admin implies all
                }
            }
            catch (IdentityNotMappedException)
            {
                // Skip unmapped groups
            }
        }
        
        return roles.Distinct().ToArray();
    }
}

// src/GBL.AX2012.MCP.Server/Security/AuthenticationMiddleware.cs
namespace GBL.AX2012.MCP.Server.Security;

public class AuthenticationMiddleware
{
    private readonly IAuthenticationService _authService;
    private readonly SecurityOptions _options;
    private readonly ILogger<AuthenticationMiddleware> _logger;
    
    public AuthenticationMiddleware(
        IAuthenticationService authService,
        IOptions<SecurityOptions> options,
        ILogger<AuthenticationMiddleware> logger)
    {
        _authService = authService;
        _options = options.Value;
        _logger = logger;
    }
    
    public async Task<ToolContext> CreateContextAsync(CancellationToken cancellationToken)
    {
        var context = new ToolContext
        {
            CorrelationId = Guid.NewGuid().ToString()
        };
        
        if (!_options.RequireAuthentication)
        {
            context.UserId = "anonymous";
            context.Roles = ["MCP_Read", "MCP_Write", "MCP_Admin"]; // All roles when auth disabled
            return context;
        }
        
        var authResult = await _authService.AuthenticateAsync(cancellationToken);
        
        if (!authResult.IsAuthenticated)
        {
            throw new UnauthorizedException("Authentication required");
        }
        
        context.UserId = authResult.UserId ?? "unknown";
        context.Roles = authResult.Roles;
        
        return context;
    }
}
```

---

## Story 5.2: Role-Based Authorization

### Implementation Plan

```
📁 Role-Based Authorization
│
├── 1. Create IAuthorizationService
│   └── Check role permissions
│
├── 2. Create AuthorizeAttribute
│   └── Declarative authorization
│
├── 3. Create tool role mapping
│   └── Tool → Required roles
│
└── 4. Integrate with tool execution
    └── Check before execute
```

### Files to Create

```csharp
// src/GBL.AX2012.MCP.Server/Security/IAuthorizationService.cs
namespace GBL.AX2012.MCP.Server.Security;

public interface IAuthorizationService
{
    bool IsAuthorized(ToolContext context, string[] requiredRoles);
    void EnsureAuthorized(ToolContext context, string[] requiredRoles);
}

// src/GBL.AX2012.MCP.Server/Security/AuthorizationService.cs
namespace GBL.AX2012.MCP.Server.Security;

public class AuthorizationService : IAuthorizationService
{
    private readonly ILogger<AuthorizationService> _logger;
    
    public AuthorizationService(ILogger<AuthorizationService> logger)
    {
        _logger = logger;
    }
    
    public bool IsAuthorized(ToolContext context, string[] requiredRoles)
    {
        if (requiredRoles == null || requiredRoles.Length == 0)
        {
            return true;
        }
        
        return requiredRoles.Any(role => context.Roles.Contains(role, StringComparer.OrdinalIgnoreCase));
    }
    
    public void EnsureAuthorized(ToolContext context, string[] requiredRoles)
    {
        if (!IsAuthorized(context, requiredRoles))
        {
            _logger.LogWarning("User {UserId} denied access. Required: {Required}, Has: {Has}",
                context.UserId, string.Join(", ", requiredRoles), string.Join(", ", context.Roles));
            
            throw new ForbiddenException(
                $"Access denied. Required roles: {string.Join(" or ", requiredRoles)}");
        }
    }
}

// src/GBL.AX2012.MCP.Server/Security/ToolRoleMapping.cs
namespace GBL.AX2012.MCP.Server.Security;

public static class ToolRoleMapping
{
    private static readonly Dictionary<string, string[]> _mapping = new()
    {
        ["ax_health_check"] = ["MCP_Read"],
        ["ax_get_customer"] = ["MCP_Read"],
        ["ax_get_salesorder"] = ["MCP_Read"],
        ["ax_check_inventory"] = ["MCP_Read"],
        ["ax_simulate_price"] = ["MCP_Read"],
        ["ax_create_salesorder"] = ["MCP_Write"],
        ["ax_update_salesorder"] = ["MCP_Write"],
        ["ax_query_audit"] = ["MCP_Admin"]
    };
    
    public static string[] GetRequiredRoles(string toolName)
    {
        return _mapping.TryGetValue(toolName, out var roles) ? roles : ["MCP_Read"];
    }
}

// src/GBL.AX2012.MCP.Core/Exceptions/ForbiddenException.cs
namespace GBL.AX2012.MCP.Core.Exceptions;

public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message) { }
}

// src/GBL.AX2012.MCP.Core/Exceptions/UnauthorizedException.cs
namespace GBL.AX2012.MCP.Core.Exceptions;

public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message) : base(message) { }
}
```

### Updated McpServer to use authorization

```csharp
// In McpServer.HandleToolCallAsync
private async Task<string> HandleToolCallAsync(McpRequest request, CancellationToken cancellationToken)
{
    var toolName = request.Params?.GetProperty("name").GetString();
    var tool = _tools.FirstOrDefault(t => t.Name == toolName);
    
    if (tool == null)
    {
        return CreateErrorResponse(request.Id, $"Tool not found: {toolName}");
    }
    
    // Create authenticated context
    var context = await _authMiddleware.CreateContextAsync(cancellationToken);
    
    // Check authorization
    var requiredRoles = ToolRoleMapping.GetRequiredRoles(toolName!);
    _authorizationService.EnsureAuthorized(context, requiredRoles);
    
    var arguments = request.Params?.GetProperty("arguments") ?? default;
    var result = await tool.ExecuteAsync(arguments, context, cancellationToken);
    
    // ... rest of method
}
```

---

## Story 5.3: Database Audit Service

### Implementation Plan

```
📁 Database Audit Service
│
├── 1. Create AuditDbContext
│   └── EF Core context
│
├── 2. Create AuditEntry entity
│   └── Database model
│
├── 3. Create DatabaseAuditService
│   └── Implements IAuditService
│
├── 4. Create migration
│   └── Database schema
│
└── 5. Unit tests
    └── Test audit logging
```

### Files to Create

```csharp
// src/GBL.AX2012.MCP.Audit/AuditDbContext.cs
namespace GBL.AX2012.MCP.Audit;

public class AuditDbContext : DbContext
{
    public AuditDbContext(DbContextOptions<AuditDbContext> options) : base(options) { }
    
    public DbSet<AuditEntryEntity> AuditEntries { get; set; } = null!;
    public DbSet<IdempotencyEntry> IdempotencyEntries { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditEntryEntity>(entity =>
        {
            entity.ToTable("AuditEntries");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ToolName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.UserId).HasMaxLength(200).IsRequired();
            entity.Property(e => e.CorrelationId).HasMaxLength(50);
            entity.Property(e => e.Input).HasColumnType("nvarchar(max)");
            entity.Property(e => e.Output).HasColumnType("nvarchar(max)");
            entity.Property(e => e.Error).HasMaxLength(2000);
            
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ToolName);
            entity.HasIndex(e => e.CorrelationId);
        });
        
        modelBuilder.Entity<IdempotencyEntry>(entity =>
        {
            entity.ToTable("IdempotencyEntries");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Key).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Data).HasColumnType("nvarchar(max)");
            
            entity.HasIndex(e => e.Key).IsUnique();
            entity.HasIndex(e => e.ExpiresAt);
        });
    }
}

// src/GBL.AX2012.MCP.Audit/Entities/AuditEntryEntity.cs
namespace GBL.AX2012.MCP.Audit.Entities;

public class AuditEntryEntity
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string UserId { get; set; } = "";
    public string ToolName { get; set; } = "";
    public string? CorrelationId { get; set; }
    public string? Input { get; set; }
    public string? Output { get; set; }
    public bool Success { get; set; }
    public long DurationMs { get; set; }
    public string? Error { get; set; }
}

// src/GBL.AX2012.MCP.Audit/DatabaseAuditService.cs
namespace GBL.AX2012.MCP.Audit;

public class DatabaseAuditService : IAuditService
{
    private readonly AuditDbContext _dbContext;
    private readonly ILogger<DatabaseAuditService> _logger;
    
    public DatabaseAuditService(AuditDbContext dbContext, ILogger<DatabaseAuditService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }
    
    public async Task LogAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        var entity = new AuditEntryEntity
        {
            Id = entry.Id,
            Timestamp = entry.Timestamp,
            UserId = entry.UserId,
            ToolName = entry.ToolName,
            CorrelationId = entry.CorrelationId,
            Input = MaskSensitiveData(entry.Input),
            Output = entry.Output,
            Success = entry.Success,
            DurationMs = entry.DurationMs,
            Error = entry.Error
        };
        
        _dbContext.AuditEntries.Add(entity);
        
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Audit entry saved: {Id}", entry.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save audit entry {Id}", entry.Id);
            // Don't throw - audit failure shouldn't break the operation
        }
    }
    
    public async Task<IEnumerable<AuditEntry>> QueryAsync(AuditQuery query, CancellationToken cancellationToken = default)
    {
        var queryable = _dbContext.AuditEntries.AsQueryable();
        
        if (!string.IsNullOrEmpty(query.UserId))
        {
            queryable = queryable.Where(e => e.UserId == query.UserId);
        }
        
        if (!string.IsNullOrEmpty(query.ToolName))
        {
            queryable = queryable.Where(e => e.ToolName == query.ToolName);
        }
        
        if (query.DateFrom.HasValue)
        {
            queryable = queryable.Where(e => e.Timestamp >= query.DateFrom.Value);
        }
        
        if (query.DateTo.HasValue)
        {
            queryable = queryable.Where(e => e.Timestamp <= query.DateTo.Value);
        }
        
        if (query.Success.HasValue)
        {
            queryable = queryable.Where(e => e.Success == query.Success.Value);
        }
        
        if (!string.IsNullOrEmpty(query.CorrelationId))
        {
            queryable = queryable.Where(e => e.CorrelationId == query.CorrelationId);
        }
        
        var entities = await queryable
            .OrderByDescending(e => e.Timestamp)
            .Skip(query.Skip)
            .Take(query.Take)
            .ToListAsync(cancellationToken);
        
        return entities.Select(e => new AuditEntry
        {
            Id = e.Id,
            Timestamp = e.Timestamp,
            UserId = e.UserId,
            ToolName = e.ToolName,
            CorrelationId = e.CorrelationId,
            Input = e.Input,
            Output = e.Output,
            Success = e.Success,
            DurationMs = e.DurationMs,
            Error = e.Error
        });
    }
    
    private string? MaskSensitiveData(string? input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        
        // Mask passwords, tokens, etc.
        var masked = Regex.Replace(input, @"""password""\s*:\s*""[^""]*""", @"""password"":""***""", RegexOptions.IgnoreCase);
        masked = Regex.Replace(masked, @"""token""\s*:\s*""[^""]*""", @"""token"":""***""", RegexOptions.IgnoreCase);
        
        return masked;
    }
}
```

### Migration Script

```sql
-- migrations/001_CreateAuditTables.sql
CREATE TABLE [dbo].[AuditEntries] (
    [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    [Timestamp] DATETIME2 NOT NULL,
    [UserId] NVARCHAR(200) NOT NULL,
    [ToolName] NVARCHAR(100) NOT NULL,
    [CorrelationId] NVARCHAR(50) NULL,
    [Input] NVARCHAR(MAX) NULL,
    [Output] NVARCHAR(MAX) NULL,
    [Success] BIT NOT NULL,
    [DurationMs] BIGINT NOT NULL,
    [Error] NVARCHAR(2000) NULL
);

CREATE INDEX [IX_AuditEntries_Timestamp] ON [dbo].[AuditEntries] ([Timestamp]);
CREATE INDEX [IX_AuditEntries_UserId] ON [dbo].[AuditEntries] ([UserId]);
CREATE INDEX [IX_AuditEntries_ToolName] ON [dbo].[AuditEntries] ([ToolName]);
CREATE INDEX [IX_AuditEntries_CorrelationId] ON [dbo].[AuditEntries] ([CorrelationId]);

CREATE TABLE [dbo].[IdempotencyEntries] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [Key] NVARCHAR(100) NOT NULL,
    [Data] NVARCHAR(MAX) NOT NULL,
    [CreatedAt] DATETIME2 NOT NULL,
    [ExpiresAt] DATETIME2 NOT NULL
);

CREATE UNIQUE INDEX [IX_IdempotencyEntries_Key] ON [dbo].[IdempotencyEntries] ([Key]);
CREATE INDEX [IX_IdempotencyEntries_ExpiresAt] ON [dbo].[IdempotencyEntries] ([ExpiresAt]);

-- Cleanup job (run daily)
-- DELETE FROM [dbo].[AuditEntries] WHERE [Timestamp] < DATEADD(DAY, -90, GETUTCDATE());
-- DELETE FROM [dbo].[IdempotencyEntries] WHERE [ExpiresAt] < GETUTCDATE();
```

---

## Story 5.4: File Audit Service

### Files to Create

```csharp
// src/GBL.AX2012.MCP.Audit/FileAuditService.cs
namespace GBL.AX2012.MCP.Audit;

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
        Directory.CreateDirectory(_options.FileLogPath);
    }
    
    public Task LogAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        var fileName = $"mcp-audit-{entry.Timestamp:yyyy-MM-dd}.jsonl";
        var filePath = Path.Combine(_options.FileLogPath, fileName);
        
        var json = JsonSerializer.Serialize(new
        {
            timestamp = entry.Timestamp.ToString("O"),
            user_id = entry.UserId,
            tool = entry.ToolName,
            correlation_id = entry.CorrelationId,
            success = entry.Success,
            duration_ms = entry.DurationMs,
            error = entry.Error
            // Note: Input/Output not logged to file for space reasons
        });
        
        lock (_lock)
        {
            File.AppendAllText(filePath, json + Environment.NewLine);
        }
        
        return Task.CompletedTask;
    }
    
    public Task<IEnumerable<AuditEntry>> QueryAsync(AuditQuery query, CancellationToken cancellationToken = default)
    {
        // File audit doesn't support querying - use database for that
        throw new NotSupportedException("File audit service does not support querying. Use database audit.");
    }
}

// src/GBL.AX2012.MCP.Audit/CompositeAuditService.cs
namespace GBL.AX2012.MCP.Audit;

public class CompositeAuditService : IAuditService
{
    private readonly DatabaseAuditService _databaseAudit;
    private readonly FileAuditService _fileAudit;
    private readonly ILogger<CompositeAuditService> _logger;
    
    // Tools that require database audit (writes)
    private static readonly HashSet<string> _writeTools = new(StringComparer.OrdinalIgnoreCase)
    {
        "ax_create_salesorder",
        "ax_update_salesorder",
        "ax_reserve_salesline"
    };
    
    public CompositeAuditService(
        DatabaseAuditService databaseAudit,
        FileAuditService fileAudit,
        ILogger<CompositeAuditService> logger)
    {
        _databaseAudit = databaseAudit;
        _fileAudit = fileAudit;
        _logger = logger;
    }
    
    public async Task LogAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        if (_writeTools.Contains(entry.ToolName))
        {
            // Write operations go to database
            await _databaseAudit.LogAsync(entry, cancellationToken);
        }
        else
        {
            // Read operations go to file
            await _fileAudit.LogAsync(entry, cancellationToken);
        }
    }
    
    public Task<IEnumerable<AuditEntry>> QueryAsync(AuditQuery query, CancellationToken cancellationToken = default)
    {
        return _databaseAudit.QueryAsync(query, cancellationToken);
    }
}
```

---

## Story 5.5: Audit Query Tool

### Files to Create

```csharp
// src/GBL.AX2012.MCP.Server/Tools/QueryAudit/QueryAuditInput.cs
namespace GBL.AX2012.MCP.Server.Tools.QueryAudit;

public class QueryAuditInput
{
    public string? UserId { get; set; }
    public string? ToolName { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public bool? Success { get; set; }
    public string? CorrelationId { get; set; }
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 50;
}

// src/GBL.AX2012.MCP.Server/Tools/QueryAudit/QueryAuditOutput.cs
namespace GBL.AX2012.MCP.Server.Tools.QueryAudit;

public class QueryAuditOutput
{
    public List<AuditEntryDto> Entries { get; set; } = new();
    public int Skip { get; set; }
    public int Take { get; set; }
    public bool HasMore { get; set; }
}

public class AuditEntryDto
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string UserId { get; set; } = "";
    public string ToolName { get; set; } = "";
    public string? CorrelationId { get; set; }
    public bool Success { get; set; }
    public long DurationMs { get; set; }
    public string? Error { get; set; }
}

// src/GBL.AX2012.MCP.Server/Tools/QueryAudit/QueryAuditTool.cs
namespace GBL.AX2012.MCP.Server.Tools.QueryAudit;

public class QueryAuditTool : ToolBase<QueryAuditInput, QueryAuditOutput>
{
    private readonly IAuditService _auditService;
    
    public override string Name => "ax_query_audit";
    public override string Description => "Query the audit log for MCP operations (Admin only)";
    
    public QueryAuditTool(
        ILogger<QueryAuditTool> logger,
        IAuditService audit)
        : base(logger, audit)
    {
        _auditService = audit;
    }
    
    protected override async Task<QueryAuditOutput> ExecuteCoreAsync(
        QueryAuditInput input, 
        ToolContext context, 
        CancellationToken cancellationToken)
    {
        var query = new AuditQuery
        {
            UserId = input.UserId,
            ToolName = input.ToolName,
            DateFrom = input.DateFrom,
            DateTo = input.DateTo,
            Success = input.Success,
            CorrelationId = input.CorrelationId,
            Skip = input.Skip,
            Take = input.Take + 1 // Get one extra to check if there's more
        };
        
        var entries = (await _auditService.QueryAsync(query, cancellationToken)).ToList();
        var hasMore = entries.Count > input.Take;
        
        if (hasMore)
        {
            entries = entries.Take(input.Take).ToList();
        }
        
        return new QueryAuditOutput
        {
            Entries = entries.Select(e => new AuditEntryDto
            {
                Id = e.Id,
                Timestamp = e.Timestamp,
                UserId = e.UserId,
                ToolName = e.ToolName,
                CorrelationId = e.CorrelationId,
                Success = e.Success,
                DurationMs = e.DurationMs,
                Error = e.Error
            }).ToList(),
            Skip = input.Skip,
            Take = input.Take,
            HasMore = hasMore
        };
    }
}
```

---

## Epic 5 Summary

| Story | Files | Tests | Status |
|-------|-------|-------|--------|
| 5.1 | WindowsAuthenticationService, AuthMiddleware | 3 unit tests | Ready |
| 5.2 | AuthorizationService, ToolRoleMapping | 3 unit tests | Ready |
| 5.3 | AuditDbContext, DatabaseAuditService | 2 unit tests | Ready |
| 5.4 | FileAuditService, CompositeAuditService | 2 unit tests | Ready |
| 5.5 | QueryAuditTool, Input, Output | 2 unit tests | Ready |

**Total:** ~15 files, ~12 unit tests
