using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using GBL.AX2012.MCP.Core.Exceptions;
using GBL.AX2012.MCP.Core.Interfaces;
using GBL.AX2012.MCP.Core.Options;

namespace GBL.AX2012.MCP.Server.Middleware;

public class CircuitBreaker : ICircuitBreaker
{
    private readonly CircuitBreakerOptions _options;
    private readonly ILogger<CircuitBreaker> _logger;
    
    private CircuitState _state = CircuitState.Closed;
    private int _failureCount;
    private DateTime _lastFailure;
    private DateTime _openedAt;
    private readonly object _lock = new();
    
    public CircuitState State => _state;
    
    public CircuitBreaker(IOptions<CircuitBreakerOptions> options, ILogger<CircuitBreaker> logger)
    {
        _options = options.Value;
        _logger = logger;
    }
    
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_state == CircuitState.Open)
            {
                var timeSinceOpen = DateTime.UtcNow - _openedAt;
                if (timeSinceOpen > _options.OpenDuration)
                {
                    _state = CircuitState.HalfOpen;
                    _logger.LogInformation("Circuit breaker transitioning to half-open");
                }
                else
                {
                    var retryAfter = _options.OpenDuration - timeSinceOpen;
                    throw new CircuitBreakerOpenException(
                        $"Circuit breaker is open. Retry after {retryAfter.TotalSeconds:F0}s",
                        retryAfter);
                }
            }
        }
        
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_options.Timeout);
            
            var result = await action();
            
            lock (_lock)
            {
                _failureCount = 0;
                if (_state == CircuitState.HalfOpen)
                {
                    _state = CircuitState.Closed;
                    _logger.LogInformation("Circuit breaker closed after successful test");
                }
            }
            
            return result;
        }
        catch (Exception ex) when (ex is not CircuitBreakerOpenException)
        {
            lock (_lock)
            {
                _failureCount++;
                _lastFailure = DateTime.UtcNow;
                
                if (_failureCount >= _options.FailureThreshold)
                {
                    _state = CircuitState.Open;
                    _openedAt = DateTime.UtcNow;
                    _logger.LogWarning("Circuit breaker opened after {Count} failures", _failureCount);
                }
            }
            
            throw;
        }
    }
    
    public async Task ExecuteAsync(Func<Task> action, CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(async () =>
        {
            await action();
            return true;
        }, cancellationToken);
    }
}
