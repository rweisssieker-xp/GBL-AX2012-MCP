using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using GBL.AX2012.MCP.Server.Events;
using GBL.AX2012.MCP.Core.Models;

namespace GBL.AX2012.MCP.Server.Webhooks;

public interface IWebhookService
{
    Task<WebhookSubscription> SubscribeAsync(WebhookSubscription subscription, CancellationToken cancellationToken = default);
    Task UnsubscribeAsync(Guid subscriptionId, CancellationToken cancellationToken = default);
    Task<List<WebhookSubscription>> ListSubscriptionsAsync(string? eventType = null, CancellationToken cancellationToken = default);
    Task TriggerWebhookAsync(string eventType, object eventData, CancellationToken cancellationToken = default);
}

public class WebhookServiceOptions
{
    public const string SectionName = "Webhooks";
    public int MaxConcurrentDeliveries { get; set; } = 10;
    public int DeliveryTimeoutSeconds { get; set; } = 30;
}

public class WebhookService : IWebhookService
{
    private readonly ILogger<WebhookService> _logger;
    private readonly HttpClient _httpClient;
    private readonly IEventBus _eventBus;
    private readonly List<WebhookSubscription> _subscriptions = new();
    private readonly SemaphoreSlim _deliverySemaphore;
    private readonly WebhookServiceOptions _options;
    
    public WebhookService(
        ILogger<WebhookService> logger,
        HttpClient httpClient,
        IEventBus eventBus,
        IOptions<WebhookServiceOptions> options)
    {
        _logger = logger;
        _httpClient = httpClient;
        _eventBus = eventBus;
        _options = options.Value;
        _deliverySemaphore = new SemaphoreSlim(_options.MaxConcurrentDeliveries);
        
        // Subscribe to events
        _eventBus.Subscribe<SalesOrderCreatedEvent>(HandleSalesOrderCreated);
        _eventBus.Subscribe<PaymentPostedEvent>(HandlePaymentPosted);
        _eventBus.Subscribe<InvoiceCreatedEvent>(HandleInvoiceCreated);
        _eventBus.Subscribe<InventoryLowStockEvent>(HandleInventoryLowStock);
    }
    
    public Task<WebhookSubscription> SubscribeAsync(WebhookSubscription subscription, CancellationToken cancellationToken = default)
    {
        subscription.Id = Guid.NewGuid();
        subscription.CreatedAt = DateTime.UtcNow;
        
        lock (_subscriptions)
        {
            _subscriptions.Add(subscription);
        }
        
        _logger.LogInformation("Webhook subscription created: {SubscriptionId} for event {EventType}", 
            subscription.Id, subscription.EventType);
        
        return Task.FromResult(subscription);
    }
    
    public Task UnsubscribeAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        lock (_subscriptions)
        {
            var subscription = _subscriptions.FirstOrDefault(s => s.Id == subscriptionId);
            if (subscription != null)
            {
                subscription.IsActive = false;
                _logger.LogInformation("Webhook subscription deactivated: {SubscriptionId}", subscriptionId);
            }
        }
        
        return Task.CompletedTask;
    }
    
    public Task<List<WebhookSubscription>> ListSubscriptionsAsync(string? eventType = null, CancellationToken cancellationToken = default)
    {
        lock (_subscriptions)
        {
            var subscriptions = _subscriptions
                .Where(s => s.IsActive && (eventType == null || s.EventType == eventType))
                .ToList();
            
            return Task.FromResult(subscriptions);
        }
    }
    
    public async Task TriggerWebhookAsync(string eventType, object eventData, CancellationToken cancellationToken = default)
    {
        List<WebhookSubscription> matchingSubscriptions;
        
        lock (_subscriptions)
        {
            matchingSubscriptions = _subscriptions
                .Where(s => s.IsActive && s.EventType == eventType)
                .ToList();
        }
        
        if (matchingSubscriptions.Count == 0)
        {
            return;
        }
        
        _logger.LogDebug("Triggering {Count} webhooks for event {EventType}", matchingSubscriptions.Count, eventType);
        
        var tasks = matchingSubscriptions.Select(sub => DeliverWebhookAsync(sub, eventData, cancellationToken));
        await Task.WhenAll(tasks);
    }
    
    private async Task DeliverWebhookAsync(WebhookSubscription subscription, object eventData, CancellationToken cancellationToken)
    {
        await _deliverySemaphore.WaitAsync(cancellationToken);
        try
        {
            var payload = new
            {
                Event = subscription.EventType,
                Timestamp = DateTime.UtcNow,
                Data = eventData
            };
            
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            // Add signature if secret is provided
            if (!string.IsNullOrEmpty(subscription.Secret))
            {
                var signature = ComputeSignature(json, subscription.Secret);
                content.Headers.Add("X-Webhook-Signature", signature);
            }
            
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_options.DeliveryTimeoutSeconds));
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);
            
            try
            {
                var response = await _httpClient.PostAsync(subscription.WebhookUrl, content, linkedCts.Token);
                
                subscription.LastTriggeredAt = DateTime.UtcNow;
                
                if (response.IsSuccessStatusCode)
                {
                    subscription.SuccessCount++;
                    _logger.LogDebug("Webhook delivered successfully: {SubscriptionId}", subscription.Id);
                }
                else
                {
                    subscription.FailureCount++;
                    _logger.LogWarning("Webhook delivery failed: {SubscriptionId}, Status: {Status}", 
                        subscription.Id, response.StatusCode);
                    
                    // Retry if configured
                    await RetryWebhookAsync(subscription, eventData, cancellationToken);
                }
            }
            catch (TaskCanceledException)
            {
                subscription.FailureCount++;
                _logger.LogWarning("Webhook delivery timeout: {SubscriptionId}", subscription.Id);
                await RetryWebhookAsync(subscription, eventData, cancellationToken);
            }
            catch (Exception ex)
            {
                subscription.FailureCount++;
                _logger.LogError(ex, "Error delivering webhook: {SubscriptionId}", subscription.Id);
                await RetryWebhookAsync(subscription, eventData, cancellationToken);
            }
        }
        finally
        {
            _deliverySemaphore.Release();
        }
    }
    
    private async Task RetryWebhookAsync(WebhookSubscription subscription, object eventData, CancellationToken cancellationToken)
    {
        if (subscription.RetryPolicy.MaxRetries <= 0)
        {
            return;
        }
        
        for (int attempt = 1; attempt <= subscription.RetryPolicy.MaxRetries; attempt++)
        {
            var delay = subscription.RetryPolicy.ExponentialBackoff
                ? subscription.RetryPolicy.BackoffMs * (int)Math.Pow(2, attempt - 1)
                : subscription.RetryPolicy.BackoffMs;
            
            await Task.Delay(delay, cancellationToken);
            
            try
            {
                var payload = new
                {
                    Event = subscription.EventType,
                    Timestamp = DateTime.UtcNow,
                    Data = eventData
                };
                
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                if (!string.IsNullOrEmpty(subscription.Secret))
                {
                    var signature = ComputeSignature(json, subscription.Secret);
                    content.Headers.Add("X-Webhook-Signature", signature);
                }
                
                var response = await _httpClient.PostAsync(subscription.WebhookUrl, content, cancellationToken);
                
                if (response.IsSuccessStatusCode)
                {
                    subscription.SuccessCount++;
                    _logger.LogInformation("Webhook delivered successfully on retry {Attempt}: {SubscriptionId}", 
                        attempt, subscription.Id);
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Webhook retry {Attempt} failed: {SubscriptionId}", attempt, subscription.Id);
            }
        }
        
        _logger.LogError("Webhook delivery failed after {MaxRetries} retries: {SubscriptionId}", 
            subscription.RetryPolicy.MaxRetries, subscription.Id);
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

