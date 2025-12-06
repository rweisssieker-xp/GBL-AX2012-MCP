# ADR-003: Resilience Patterns

**Status:** Accepted  
**Date:** 2025-12-06  
**Decision Makers:** Reinerw

## Context

The MCP Server depends on AX 2012 services which can:
- Timeout under load
- Fail during maintenance windows
- Return errors due to data issues
- Become unavailable due to infrastructure problems

We need resilience patterns to handle these failures gracefully.

## Decision

### Pattern 1: Circuit Breaker

Prevents cascading failures when AX is unhealthy.

```
Configuration:
- Failure Threshold: 3 consecutive failures
- Open Duration: 60 seconds
- Half-Open: Allow 1 test request
- Timeout: 30 seconds per request
```

State Machine:
```
┌────────┐  3 failures   ┌────────┐  60s elapsed  ┌───────────┐
│ CLOSED │ ────────────> │  OPEN  │ ────────────> │ HALF-OPEN │
└────────┘               └────────┘               └───────────┘
    ▲                         │                        │
    │                         │ immediate              │ 1 success
    │                         │ rejection              │
    │                         ▼                        ▼
    │                    ┌─────────┐              ┌────────┐
    └────────────────────│ REJECT  │              │ CLOSED │
         success         └─────────┘              └────────┘
```

### Pattern 2: Retry with Exponential Backoff

Handles transient failures.

```
Configuration:
- Max Retries: 2
- Initial Delay: 100ms
- Max Delay: 2s
- Backoff Multiplier: 2
- Jitter: ±10%
```

Retry Schedule:
```
Attempt 1: Immediate
Attempt 2: 100ms ± 10ms
Attempt 3: 200ms ± 20ms
(Give up after 3 attempts)
```

Retryable Errors:
- Timeout
- Connection refused
- 503 Service Unavailable
- 429 Too Many Requests

Non-Retryable Errors:
- 400 Bad Request
- 401 Unauthorized
- 404 Not Found
- 409 Conflict (idempotency)

### Pattern 3: Rate Limiting

Protects AX from overload.

```
Configuration:
- Requests per Minute: 100 per user
- Algorithm: Token Bucket
- Burst: 10 requests
```

Response when limited:
```json
{
  "error": "RATE_LIMITED",
  "message": "Too many requests. Please slow down.",
  "retry_after_seconds": 30
}
```

### Pattern 4: Idempotency

Ensures write operations are safe to retry.

```
Configuration:
- Key Format: Client-provided UUID
- TTL: 7 days
- Storage: Distributed cache (Redis)
```

Behavior:
```
Request 1 (key=abc): Execute → Store result → Return result
Request 2 (key=abc): Lookup → Return cached result (no execution)
Request 3 (key=xyz): Execute → Store result → Return result
```

### Pattern 5: Timeout Hierarchy

Layered timeouts prevent hanging requests.

```
┌─────────────────────────────────────────────────────────────┐
│ Client Timeout: 60s                                          │
│  ┌─────────────────────────────────────────────────────────┐│
│  │ MCP Server Timeout: 45s                                 ││
│  │  ┌─────────────────────────────────────────────────────┐││
│  │  │ Circuit Breaker Timeout: 30s                        │││
│  │  │  ┌─────────────────────────────────────────────────┐│││
│  │  │  │ AX Service Timeout: 25s                         ││││
│  │  │  └─────────────────────────────────────────────────┘│││
│  │  └─────────────────────────────────────────────────────┘││
│  └─────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────┘
```

### Pattern 6: Health Checks

Proactive monitoring of dependencies.

```
Endpoints:
- /health/live    → MCP Server is running
- /health/ready   → MCP Server can serve requests
- /health         → Detailed component status

Check Interval: 30 seconds
Failure Threshold: 3 consecutive failures → Alert
```

## Implementation

```csharp
// Polly policy composition
var retryPolicy = Policy
    .Handle<HttpRequestException>()
    .Or<TimeoutException>()
    .WaitAndRetryAsync(
        retryCount: 2,
        sleepDurationProvider: attempt => 
            TimeSpan.FromMilliseconds(100 * Math.Pow(2, attempt - 1)),
        onRetry: (exception, timespan, attempt, context) =>
            _logger.LogWarning("Retry {Attempt} after {Delay}ms", attempt, timespan.TotalMilliseconds));

var circuitBreakerPolicy = Policy
    .Handle<Exception>()
    .CircuitBreakerAsync(
        exceptionsAllowedBeforeBreaking: 3,
        durationOfBreak: TimeSpan.FromSeconds(60),
        onBreak: (exception, duration) =>
            _logger.LogError("Circuit breaker opened for {Duration}s", duration.TotalSeconds),
        onReset: () =>
            _logger.LogInformation("Circuit breaker reset"));

var timeoutPolicy = Policy
    .TimeoutAsync(TimeSpan.FromSeconds(30));

// Combine policies: Timeout → Retry → Circuit Breaker
var resilientPolicy = Policy.WrapAsync(timeoutPolicy, retryPolicy, circuitBreakerPolicy);
```

## Consequences

### Positive

- **Graceful Degradation:** System remains responsive during AX issues
- **Self-Healing:** Automatic recovery when AX becomes healthy
- **Protection:** AX protected from overload
- **Observability:** Clear metrics on failure patterns

### Negative

- **Latency:** Retries add latency for failed requests
- **Complexity:** Multiple patterns to configure and monitor
- **Cache Overhead:** Idempotency store requires additional infrastructure

### Monitoring

| Metric | Alert Threshold |
|--------|-----------------|
| Circuit breaker open | Any occurrence |
| Retry rate | >10% of requests |
| Rate limit hits | >5% of requests |
| Timeout rate | >5% of requests |
| Error rate | >2% of requests |

## Alternatives Considered

### Option A: No Resilience
- **Pros:** Simple
- **Cons:** Cascading failures, poor user experience
- **Rejected:** Unacceptable for production system

### Option B: Queue-Based
- **Pros:** Decoupled, guaranteed delivery
- **Cons:** Eventual consistency, complex error handling
- **Rejected:** Overkill for synchronous operations

## References

- [Microsoft Resilience Patterns](https://docs.microsoft.com/en-us/azure/architecture/patterns/category/resiliency)
- [Polly Documentation](https://github.com/App-vNext/Polly)
- [Circuit Breaker Pattern](https://martinfowler.com/bliki/CircuitBreaker.html)
