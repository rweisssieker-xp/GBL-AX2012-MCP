using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using GBL.AX2012.MCP.Core.Interfaces;

namespace GBL.AX2012.MCP.Server.Middleware;

public class MemoryIdempotencyStore : IIdempotencyStore
{
    private readonly ConcurrentDictionary<string, CachedResult> _cache = new();
    private readonly ILogger<MemoryIdempotencyStore> _logger;
    
    public MemoryIdempotencyStore(ILogger<MemoryIdempotencyStore> logger)
    {
        _logger = logger;
    }
    
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        if (_cache.TryGetValue(key, out var cached))
        {
            if (cached.ExpiresAt > DateTime.UtcNow)
            {
                _logger.LogDebug("Idempotency cache hit for key {Key}", key);
                return Task.FromResult(JsonSerializer.Deserialize<T>(cached.Data));
            }
            
            // Expired, remove it
            _cache.TryRemove(key, out _);
        }
        
        return Task.FromResult<T?>(null);
    }
    
    public Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken = default) where T : class
    {
        var cached = new CachedResult
        {
            Data = JsonSerializer.Serialize(value),
            ExpiresAt = DateTime.UtcNow + expiration
        };
        
        _cache[key] = cached;
        _logger.LogDebug("Idempotency cache set for key {Key}, expires at {ExpiresAt}", key, cached.ExpiresAt);
        
        return Task.CompletedTask;
    }
    
    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(key, out var cached))
        {
            return Task.FromResult(cached.ExpiresAt > DateTime.UtcNow);
        }
        return Task.FromResult(false);
    }
    
    private class CachedResult
    {
        public string Data { get; set; } = "";
        public DateTime ExpiresAt { get; set; }
    }
}
