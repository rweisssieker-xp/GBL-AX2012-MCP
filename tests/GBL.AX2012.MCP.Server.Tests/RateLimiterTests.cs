using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using FluentAssertions;
using GBL.AX2012.MCP.Core.Options;
using GBL.AX2012.MCP.Server.Middleware;

namespace GBL.AX2012.MCP.Server.Tests;

public class RateLimiterTests
{
    [Fact]
    public async Task TryAcquire_UnderLimit_ReturnsTrue()
    {
        var options = Options.Create(new RateLimiterOptions { RequestsPerMinute = 10, Enabled = true });
        var limiter = new RateLimiter(options, Mock.Of<ILogger<RateLimiter>>());
        
        var result = await limiter.TryAcquireAsync("user1");
        
        result.Should().BeTrue();
    }
    
    [Fact]
    public async Task TryAcquire_OverLimit_ReturnsFalse()
    {
        var options = Options.Create(new RateLimiterOptions { RequestsPerMinute = 2, Enabled = true });
        var limiter = new RateLimiter(options, Mock.Of<ILogger<RateLimiter>>());
        
        await limiter.TryAcquireAsync("user1"); // 1
        await limiter.TryAcquireAsync("user1"); // 2
        var result = await limiter.TryAcquireAsync("user1"); // 3 - should fail
        
        result.Should().BeFalse();
    }
    
    [Fact]
    public async Task TryAcquire_Disabled_AlwaysReturnsTrue()
    {
        var options = Options.Create(new RateLimiterOptions { RequestsPerMinute = 1, Enabled = false });
        var limiter = new RateLimiter(options, Mock.Of<ILogger<RateLimiter>>());
        
        for (int i = 0; i < 100; i++)
        {
            var result = await limiter.TryAcquireAsync("user1");
            result.Should().BeTrue();
        }
    }
    
    [Fact]
    public async Task TryAcquire_DifferentUsers_IndependentLimits()
    {
        var options = Options.Create(new RateLimiterOptions { RequestsPerMinute = 2, Enabled = true });
        var limiter = new RateLimiter(options, Mock.Of<ILogger<RateLimiter>>());
        
        await limiter.TryAcquireAsync("user1");
        await limiter.TryAcquireAsync("user1");
        var user1Result = await limiter.TryAcquireAsync("user1");
        var user2Result = await limiter.TryAcquireAsync("user2");
        
        user1Result.Should().BeFalse();
        user2Result.Should().BeTrue();
    }
    
    [Fact]
    public async Task GetInfo_ReturnsCorrectRemaining()
    {
        var options = Options.Create(new RateLimiterOptions { RequestsPerMinute = 10, Enabled = true });
        var limiter = new RateLimiter(options, Mock.Of<ILogger<RateLimiter>>());
        
        await limiter.TryAcquireAsync("user1");
        await limiter.TryAcquireAsync("user1");
        await limiter.TryAcquireAsync("user1");
        
        var info = await limiter.GetInfoAsync("user1");
        
        info.Remaining.Should().BeLessThanOrEqualTo(7);
    }
}
