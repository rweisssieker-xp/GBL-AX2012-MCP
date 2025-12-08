using Microsoft.EntityFrameworkCore;
using GBL.AX2012.MCP.Server.Webhooks;

namespace GBL.AX2012.MCP.Audit.Data;

public class WebhookDbContext : DbContext
{
    public WebhookDbContext(DbContextOptions<WebhookDbContext> options) : base(options)
    {
    }
    
    public DbSet<WebhookSubscriptionEntity> WebhookSubscriptions { get; set; } = null!;
    public DbSet<WebhookDeliveryEntity> WebhookDeliveries { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WebhookSubscriptionEntity>(entity =>
        {
            entity.ToTable("WebhookSubscriptions");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.EventType).HasMaxLength(128).IsRequired();
            entity.Property(e => e.WebhookUrl).HasMaxLength(512).IsRequired();
            entity.Property(e => e.Secret).HasMaxLength(256);
            entity.Property(e => e.Filters).HasColumnType("nvarchar(max)");
            entity.Property(e => e.IsActive).IsRequired();
            
            entity.HasIndex(e => e.EventType);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => new { e.EventType, e.IsActive });
        });
        
        modelBuilder.Entity<WebhookDeliveryEntity>(entity =>
        {
            entity.ToTable("WebhookDeliveries");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.SubscriptionId).IsRequired();
            entity.Property(e => e.EventType).HasMaxLength(128).IsRequired();
            entity.Property(e => e.Payload).HasColumnType("nvarchar(max)").IsRequired();
            entity.Property(e => e.Status).HasMaxLength(32).IsRequired();
            entity.Property(e => e.ErrorMessage).HasMaxLength(4000);
            
            entity.HasIndex(e => e.SubscriptionId);
            entity.HasIndex(e => e.EventType);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.DeliveredAt);
            
            entity.HasOne<WebhookSubscriptionEntity>()
                .WithMany()
                .HasForeignKey(e => e.SubscriptionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

public class WebhookSubscriptionEntity
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = "";
    public string WebhookUrl { get; set; } = "";
    public string? Secret { get; set; }
    public string? Filters { get; set; } // JSON
    public int MaxRetries { get; set; }
    public int BackoffMs { get; set; }
    public bool ExponentialBackoff { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastTriggeredAt { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    
    public WebhookSubscription ToModel() => new()
    {
        Id = Id,
        EventType = EventType,
        WebhookUrl = WebhookUrl,
        Secret = Secret,
        Filters = string.IsNullOrEmpty(Filters) ? null : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(Filters),
        RetryPolicy = new WebhookRetryPolicy
        {
            MaxRetries = MaxRetries,
            BackoffMs = BackoffMs,
            ExponentialBackoff = ExponentialBackoff
        },
        IsActive = IsActive,
        CreatedAt = CreatedAt,
        LastTriggeredAt = LastTriggeredAt,
        SuccessCount = SuccessCount,
        FailureCount = FailureCount
    };
    
    public static WebhookSubscriptionEntity FromModel(WebhookSubscription model) => new()
    {
        Id = model.Id,
        EventType = model.EventType,
        WebhookUrl = model.WebhookUrl,
        Secret = model.Secret,
        Filters = model.Filters == null ? null : System.Text.Json.JsonSerializer.Serialize(model.Filters),
        MaxRetries = model.RetryPolicy.MaxRetries,
        BackoffMs = model.RetryPolicy.BackoffMs,
        ExponentialBackoff = model.RetryPolicy.ExponentialBackoff,
        IsActive = model.IsActive,
        CreatedAt = model.CreatedAt,
        LastTriggeredAt = model.LastTriggeredAt,
        SuccessCount = model.SuccessCount,
        FailureCount = model.FailureCount
    };
}

public class WebhookDeliveryEntity
{
    public Guid Id { get; set; }
    public Guid SubscriptionId { get; set; }
    public string EventType { get; set; } = "";
    public string Payload { get; set; } = "";
    public string Status { get; set; } = ""; // "pending", "delivered", "failed"
    public int Attempt { get; set; }
    public int? HttpStatusCode { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime DeliveredAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

