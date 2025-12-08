using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using GBL.AX2012.MCP.Server.Events;
using GBL.AX2012.MCP.Audit.Data;
using GBL.AX2012.MCP.Core.Models;

namespace GBL.AX2012.MCP.Server.Webhooks;

public class DatabaseWebhookService : IWebhookService
{
    private readonly ILogger<DatabaseWebhookService> _logger;
    private readonly HttpClient _httpClient;
    private readonly IEventBus _eventBus;
    private readonly IDbContextFactory<WebhookDbContext> _contextFactory;
    private readonly SemaphoreSlim _deliverySemaphore;
    private readonly WebhookServiceOptions _options;
    
    public DatabaseWebhookService(
        ILogger<DatabaseWebhookService> logger,
        HttpClient httpClient,
        IEventBus eventBus,
        IDbContextFactory<WebhookDbContext> contextFactory,
        IOptions<WebhookServiceOptions> options)
    {
        _logger = logger;
        _httpClient = httpClient;
        _eventBus = eventBus;
        _contextFactory = contextFactory;
        _options = options.Value;
        _deliverySemaphore = new SemaphoreSlim(_options.MaxConcurrentDeliveries);
        
        // Subscribe to events
        _eventBus.Subscribe<SalesOrderCreatedEvent>(HandleSalesOrderCreated);
        _eventBus.Subscribe<PaymentPostedEvent>(HandlePaymentPosted);
        _eventBus.Subscribe<InvoiceCreatedEvent>(HandleInvoiceCreated);
        _eventBus.Subscribe<InventoryLowStockEvent>(HandleInventoryLowStock);
    }
    
    public async Task<WebhookSubscription> SubscribeAsync(WebhookSubscription subscription, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        
        subscription.Id = Guid.NewGuid();
        subscription.CreatedAt = DateTime.UtcNow;
        
        var entity = WebhookSubscriptionEntity.FromModel(subscription);
        context.WebhookSubscriptions.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Webhook subscription created: {SubscriptionId} for event {EventType}", 
            subscription.Id, subscription.EventType);
        
        return subscription;
    }
    
    public async Task UnsubscribeAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        
        var subscription = await context.WebhookSubscriptions.FindAsync(new object[] { subscriptionId }, cancellationToken);
        if (subscription != null)
        {
            subscription.IsActive = false;
            await context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Webhook subscription deactivated: {SubscriptionId}", subscriptionId);
        }
    }
    
    public async Task<List<WebhookSubscription>> ListSubscriptionsAsync(string? eventType = null, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        
        var query = context.WebhookSubscriptions.Where(s => s.IsActive);
        
        if (!string.IsNullOrEmpty(eventType))
        {
            query = query.Where(s => s.EventType == eventType);
        }
        
        var subscriptions = await query.ToListAsync(cancellationToken);
        return subscriptions.Select(s => s.ToModel()).ToList();
    }
    
    public async Task TriggerWebhookAsync(string eventType, object eventData, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        
        var subscriptions = await context.WebhookSubscriptions
            .Where(s => s.IsActive && s.EventType == eventType)
            .ToListAsync(cancellationToken);
        
        if (subscriptions.Count == 0)
        {
            return;
        }
        
        _logger.LogDebug("Triggering {Count} webhooks for event {EventType}", subscriptions.Count, eventType);
        
        var payload = JsonSerializer.Serialize(new
        {
            Event = eventType,
            Timestamp = DateTime.UtcNow,
            Data = eventData
        });
        
        var tasks = subscriptions.Select(sub => DeliverWebhookAsync(sub, payload, cancellationToken));
        await Task.WhenAll(tasks);
    }
    
    private async Task DeliverWebhookAsync(WebhookSubscriptionEntity subscription, string payload, CancellationToken cancellationToken)
    {
        await _deliverySemaphore.WaitAsync(cancellationToken);
        
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        
        var delivery = new WebhookDeliveryEntity
        {
            Id = Guid.NewGuid(),
            SubscriptionId = subscription.Id,
            EventType = subscription.EventType,
            Payload = payload,
            Status = "pending",
            Attempt = 1,
            DeliveredAt = DateTime.UtcNow
        };
        
        context.WebhookDeliveries.Add(delivery);
        await context.SaveChangesAsync(cancellationToken);
        
        try
        {
            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            
            if (!string.IsNullOrEmpty(subscription.Secret))
            {
                var signature = ComputeSignature(payload, subscription.Secret);
                content.Headers.Add("X-Webhook-Signature", signature);
            }
            
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_options.DeliveryTimeoutSeconds));
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);
            
            try
            {
                var response = await _httpClient.PostAsync(subscription.WebhookUrl, content, linkedCts.Token);
                
                delivery.Status = response.IsSuccessStatusCode ? "delivered" : "failed";
                delivery.HttpStatusCode = (int)response.StatusCode;
                delivery.CompletedAt = DateTime.UtcNow;
                
                if (response.IsSuccessStatusCode)
                {
                    subscription.SuccessCount++;
                    subscription.LastTriggeredAt = DateTime.UtcNow;
                }
                else
                {
                    subscription.FailureCount++;
                    delivery.ErrorMessage = $"HTTP {response.StatusCode}";
                    
                    // Retry if configured
                    if (delivery.Attempt < subscription.MaxRetries)
                    {
                        await RetryWebhookAsync(subscription, payload, delivery, cancellationToken);
                    }
                }
            }
            catch (TaskCanceledException)
            {
                delivery.Status = "failed";
                delivery.ErrorMessage = "Timeout";
                delivery.CompletedAt = DateTime.UtcNow;
                subscription.FailureCount++;
                
                if (delivery.Attempt < subscription.MaxRetries)
                {
                    await RetryWebhookAsync(subscription, payload, delivery, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                delivery.Status = "failed";
                delivery.ErrorMessage = ex.Message;
                delivery.CompletedAt = DateTime.UtcNow;
                subscription.FailureCount++;
                
                if (delivery.Attempt < subscription.MaxRetries)
                {
                    await RetryWebhookAsync(subscription, payload, delivery, cancellationToken);
                }
            }
            
            await context.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            _deliverySemaphore.Release();
        }
    }
    
    private async Task RetryWebhookAsync(WebhookSubscriptionEntity subscription, string payload, WebhookDeliveryEntity delivery, CancellationToken cancellationToken)
    {
        var delay = subscription.ExponentialBackoff
            ? subscription.BackoffMs * (int)Math.Pow(2, delivery.Attempt - 1)
            : subscription.BackoffMs;
        
        await Task.Delay(delay, cancellationToken);
        
        delivery.Attempt++;
        delivery.DeliveredAt = DateTime.UtcNow;
        
        try
        {
            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            
            if (!string.IsNullOrEmpty(subscription.Secret))
            {
                var signature = ComputeSignature(payload, subscription.Secret);
                content.Headers.Add("X-Webhook-Signature", signature);
            }
            
            var response = await _httpClient.PostAsync(subscription.WebhookUrl, content, cancellationToken);
            
            delivery.Status = response.IsSuccessStatusCode ? "delivered" : "failed";
            delivery.HttpStatusCode = (int)response.StatusCode;
            delivery.CompletedAt = DateTime.UtcNow;
            
            if (response.IsSuccessStatusCode)
            {
                subscription.SuccessCount++;
                subscription.LastTriggeredAt = DateTime.UtcNow;
            }
            else
            {
                delivery.ErrorMessage = $"HTTP {response.StatusCode}";
            }
        }
        catch (Exception ex)
        {
            delivery.Status = "failed";
            delivery.ErrorMessage = ex.Message;
            delivery.CompletedAt = DateTime.UtcNow;
        }
    }
    
    private string ComputeSignature(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
    
    // Event Handlers
    private async Task HandleSalesOrderCreated(SalesOrderCreatedEvent evt, CancellationToken cancellationToken)
    {
        await TriggerWebhookAsync("salesorder.created", evt, cancellationToken);
    }
    
    private async Task HandlePaymentPosted(PaymentPostedEvent evt, CancellationToken cancellationToken)
    {
        await TriggerWebhookAsync("payment.posted", evt, cancellationToken);
    }
    
    private async Task HandleInvoiceCreated(InvoiceCreatedEvent evt, CancellationToken cancellationToken)
    {
        await TriggerWebhookAsync("invoice.created", evt, cancellationToken);
    }
    
    private async Task HandleInventoryLowStock(InventoryLowStockEvent evt, CancellationToken cancellationToken)
    {
        await TriggerWebhookAsync("inventory.low_stock", evt, cancellationToken);
    }
}

