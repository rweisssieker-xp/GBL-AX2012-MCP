using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using FluentAssertions;
using GBL.AX2012.MCP.Core.Exceptions;
using GBL.AX2012.MCP.Core.Interfaces;
using GBL.AX2012.MCP.Core.Options;
using GBL.AX2012.MCP.Server.Middleware;

namespace GBL.AX2012.MCP.Server.Tests;

public class CircuitBreakerTests
{
    [Fact]
    public async Task Execute_Success_StaysClosed()
    {
        var options = Options.Create(new CircuitBreakerOptions { FailureThreshold = 3 });
        var cb = new CircuitBreaker(options, Mock.Of<ILogger<CircuitBreaker>>());
        
        var result = await cb.ExecuteAsync(() => Task.FromResult(42));
        
        result.Should().Be(42);
        cb.State.Should().Be(CircuitState.Closed);
    }
    
    [Fact]
    public async Task Execute_ThreeFailures_Opens()
    {
        var options = Options.Create(new CircuitBreakerOptions 
        { 
            FailureThreshold = 3,
            OpenDuration = TimeSpan.FromMinutes(1)
        });
        var cb = new CircuitBreaker(options, Mock.Of<ILogger<CircuitBreaker>>());
        
        for (int i = 0; i < 3; i++)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                cb.ExecuteAsync<int>(() => throw new InvalidOperationException()));
        }
        
        cb.State.Should().Be(CircuitState.Open);
    }
    
    [Fact]
    public async Task Execute_WhenOpen_ThrowsCircuitBreakerOpenException()
    {
        var options = Options.Create(new CircuitBreakerOptions 
        { 
            FailureThreshold = 1,
            OpenDuration = TimeSpan.FromMinutes(1)
        });
        var cb = new CircuitBreaker(options, Mock.Of<ILogger<CircuitBreaker>>());
        
        // Open the circuit
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            cb.ExecuteAsync<int>(() => throw new InvalidOperationException()));
        
        // Should throw CircuitBreakerOpenException
        await Assert.ThrowsAsync<CircuitBreakerOpenException>(() => 
            cb.ExecuteAsync(() => Task.FromResult(42)));
    }
    
    [Fact]
    public async Task Execute_SuccessAfterFailures_ResetsCount()
    {
        var options = Options.Create(new CircuitBreakerOptions 
        { 
            FailureThreshold = 3,
            OpenDuration = TimeSpan.FromMinutes(1)
        });
        var cb = new CircuitBreaker(options, Mock.Of<ILogger<CircuitBreaker>>());
        
        // Two failures
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            cb.ExecuteAsync<int>(() => throw new InvalidOperationException()));
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            cb.ExecuteAsync<int>(() => throw new InvalidOperationException()));
        
        // Success resets
        await cb.ExecuteAsync(() => Task.FromResult(42));
        
        // Two more failures should not open
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            cb.ExecuteAsync<int>(() => throw new InvalidOperationException()));
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            cb.ExecuteAsync<int>(() => throw new InvalidOperationException()));
        
        cb.State.Should().Be(CircuitState.Closed);
    }
}
