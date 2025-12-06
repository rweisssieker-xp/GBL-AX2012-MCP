namespace GBL.AX2012.MCP.Core.Interfaces;

public interface IIdempotencyStore
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken = default) where T : class;
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
}
