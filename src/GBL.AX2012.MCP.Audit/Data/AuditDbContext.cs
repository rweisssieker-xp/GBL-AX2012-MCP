using Microsoft.EntityFrameworkCore;
using GBL.AX2012.MCP.Core.Models;

namespace GBL.AX2012.MCP.Audit.Data;

public class AuditDbContext : DbContext
{
    public AuditDbContext(DbContextOptions<AuditDbContext> options) : base(options)
    {
    }
    
    public DbSet<AuditEntryEntity> AuditEntries { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditEntryEntity>(entity =>
        {
            entity.ToTable("AuditEntries");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Timestamp).IsRequired();
            entity.Property(e => e.UserId).HasMaxLength(256).IsRequired();
            entity.Property(e => e.ToolName).HasMaxLength(128).IsRequired();
            entity.Property(e => e.CorrelationId).HasMaxLength(64);
            entity.Property(e => e.Input).HasColumnType("nvarchar(max)");
            entity.Property(e => e.Output).HasColumnType("nvarchar(max)");
            entity.Property(e => e.Error).HasMaxLength(4000);
            
            // Indexes for common queries
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ToolName);
            entity.HasIndex(e => e.CorrelationId);
            entity.HasIndex(e => new { e.Timestamp, e.UserId });
        });
    }
}

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
    
    public AuditEntry ToModel() => new()
    {
        Id = Id,
        Timestamp = Timestamp,
        UserId = UserId,
        ToolName = ToolName,
        CorrelationId = CorrelationId,
        Input = Input,
        Output = Output,
        Success = Success,
        DurationMs = DurationMs,
        Error = Error
    };
    
    public static AuditEntryEntity FromModel(AuditEntry model) => new()
    {
        Id = model.Id,
        Timestamp = model.Timestamp,
        UserId = model.UserId,
        ToolName = model.ToolName,
        CorrelationId = model.CorrelationId,
        Input = model.Input,
        Output = model.Output,
        Success = model.Success,
        DurationMs = model.DurationMs,
        Error = model.Error
    };
}
