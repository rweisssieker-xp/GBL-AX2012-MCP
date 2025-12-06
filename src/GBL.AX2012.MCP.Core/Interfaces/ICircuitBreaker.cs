namespace GBL.AX2012.MCP.Core.Interfaces;

public interface ICircuitBreaker
{
    Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default);
    Task ExecuteAsync(Func<Task> action, CancellationToken cancellationToken = default);
    CircuitState State { get; }
}

public enum CircuitState 
{ 
    Closed, 
    Open, 
    HalfOpen 
}
