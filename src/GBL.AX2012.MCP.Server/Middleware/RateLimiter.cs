using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using GBL.AX2012.MCP.Core.Interfaces;
using GBL.AX2012.MCP.Core.Options;

namespace GBL.AX2012.MCP.Server.Middleware;

public class RateLimiter : IRateLimiter
{
    private readonly ConcurrentDictionary<string, TokenBucket> _buckets = new();
    private readonly RateLimiterOptions _options;
    private readonly ILogger<RateLimiter> _logger;
    
    public RateLimiter(IOptions<RateLimiterOptions> options, ILogger<RateLimiter> logger)
    {
        _options = options.Value;
        _logger = logger;
    }
    
    public Task<bool> TryAcquireAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
            return Task.FromResult(true);
        
        var bucket = _buckets.GetOrAdd(userId, _ => new TokenBucket(
            _options.RequestsPerMinute,
            _options.RequestsPerMinute,
            TimeSpan.FromMinutes(1)));
        
        var acquired = bucket.TryConsume(1);
        
        if (!acquired)
        {
            _logger.LogWarning("Rate limit exceeded for user {UserId}", userId);
        }
        
        return Task.FromResult(acquired);
    }
    
    public Task<RateLimitInfo> GetInfoAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (_buckets.TryGetValue(userId, out var bucket))
        {
            return Task.FromResult(new RateLimitInfo(bucket.Remaining, bucket.ResetIn));
        }
        
        return Task.FromResult(new RateLimitInfo(_options.RequestsPerMinute, TimeSpan.FromMinutes(1)));
    }
}

internal class TokenBucket
{
    private readonly int _maxTokens;
    private readonly int _refillRate;
    private readonly TimeSpan _refillInterval;
    private double _tokens;
    private DateTime _lastRefill;
    private readonly object _lock = new();
    
    public TokenBucket(int maxTokens, int refillRate, TimeSpan refillInterval)
    {
        _maxTokens = maxTokens;
        _refillRate = refillRate;
        _refillInterval = refillInterval;
        _tokens = maxTokens;
        _lastRefill = DateTime.UtcNow;
    }
    
    public int Remaining => (int)_tokens;
    
    public TimeSpan ResetIn
    {
        get
        {
            lock (_lock)
            {
                var elapsed = DateTime.UtcNow - _lastRefill;
                var remaining = _refillInterval - elapsed;
                return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
            }
        }
    }
    
    public bool TryConsume(int tokens)
    {
        lock (_lock)
        {
            Refill();
            
            if (_tokens >= tokens)
            {
                _tokens -= tokens;
                return true;
            }
            
            return false;
        }
    }
    
    private void Refill()
    {
        var now = DateTime.UtcNow;
        var elapsed = now - _lastRefill;
        var tokensToAdd = (elapsed.TotalMilliseconds / _refillInterval.TotalMilliseconds) * _refillRate;
        
        _tokens = Math.Min(_maxTokens, _tokens + tokensToAdd);
        _lastRefill = now;
    }
}
