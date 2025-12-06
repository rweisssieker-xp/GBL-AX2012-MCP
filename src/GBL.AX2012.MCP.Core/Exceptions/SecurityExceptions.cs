namespace GBL.AX2012.MCP.Core.Exceptions;

public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message) : base(message) { }
}

public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message) { }
}

public class CircuitBreakerOpenException : Exception
{
    public TimeSpan RetryAfter { get; }
    
    public CircuitBreakerOpenException(string message, TimeSpan retryAfter) : base(message)
    {
        RetryAfter = retryAfter;
    }
}
