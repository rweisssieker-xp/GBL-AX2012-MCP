namespace GBL.AX2012.MCP.Core.Interfaces;

public interface IRateLimiter
{
    Task<bool> TryAcquireAsync(string userId, CancellationToken cancellationToken = default);
    Task<RateLimitInfo> GetInfoAsync(string userId, CancellationToken cancellationToken = default);
}

public record RateLimitInfo(int Remaining, TimeSpan ResetIn);
