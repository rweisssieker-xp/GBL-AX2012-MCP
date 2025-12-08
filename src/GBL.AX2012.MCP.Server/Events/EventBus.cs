using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using GBL.AX2012.MCP.Core.Interfaces;

namespace GBL.AX2012.MCP.Server.Events;

public interface IEventBus
{
    Task PublishAsync<T>(T eventData, CancellationToken cancellationToken = default) where T : class;
    void Subscribe<T>(Func<T, CancellationToken, Task> handler) where T : class;
}

public class EventBus : IEventBus
{
    private readonly ILogger<EventBus> _logger;
    private readonly ConcurrentDictionary<Type, List<object>> _handlers = new();
    
    public EventBus(ILogger<EventBus> logger)
    {
        _logger = logger;
    }
    
    public void Subscribe<T>(Func<T, CancellationToken, Task> handler) where T : class
    {
        var handlers = _handlers.GetOrAdd(typeof(T), _ => new List<object>());
        lock (handlers)
        {
            handlers.Add(handler);
        }
        
        _logger.LogDebug("Subscribed handler for event type {EventType}", typeof(T).Name);
    }
    
    public async Task PublishAsync<T>(T eventData, CancellationToken cancellationToken = default) where T : class
    {
        if (!_handlers.TryGetValue(typeof(T), out var handlers))
        {
            return;
        }
        
        var typedHandlers = handlers.Cast<Func<T, CancellationToken, Task>>().ToList();
        
        _logger.LogDebug("Publishing event {EventType} to {HandlerCount} handlers", typeof(T).Name, typedHandlers.Count);
        
        // Execute handlers in parallel (fire and forget for non-critical handlers)
        var tasks = typedHandlers.Select(handler =>
        {
            try
            {
                return handler(eventData, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in event handler for {EventType}", typeof(T).Name);
                return Task.CompletedTask;
            }
        });
        
        await Task.WhenAll(tasks);
    }
}

// Event Types
public class SalesOrderCreatedEvent
{
    public string SalesId { get; set; } = "";
    public string CustomerAccount { get; set; } = "";
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public string UserId { get; set; } = "";
}

public class SalesOrderUpdatedEvent
{
    public string SalesId { get; set; } = "";
    public string CustomerAccount { get; set; } = "";
    public DateTime UpdatedAt { get; set; }
    public string UserId { get; set; } = "";
}

public class PaymentPostedEvent
{
    public string PaymentId { get; set; } = "";
    public string CustomerAccount { get; set; } = "";
    public decimal Amount { get; set; }
    public DateTime PostedAt { get; set; }
    public string UserId { get; set; } = "";
}

public class InvoiceCreatedEvent
{
    public string InvoiceId { get; set; } = "";
    public string CustomerAccount { get; set; } = "";
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
    public string UserId { get; set; } = "";
}

public class InventoryLowStockEvent
{
    public string ItemId { get; set; } = "";
    public string Warehouse { get; set; } = "";
    public decimal AvailableQuantity { get; set; }
    public decimal Threshold { get; set; }
    public DateTime DetectedAt { get; set; }
}

