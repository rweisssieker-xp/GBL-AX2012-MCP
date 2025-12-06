using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using FluentAssertions;
using GBL.AX2012.MCP.Core.Models;
using GBL.AX2012.MCP.Server.Tools;

namespace GBL.AX2012.MCP.Integration.Tests;

public class HealthCheckTests : IClassFixture<TestFixture>
{
    private readonly TestFixture _fixture;
    
    public HealthCheckTests(TestFixture fixture)
    {
        _fixture = fixture;
    }
    
    [Fact]
    public async Task HealthCheck_Basic_ReturnsHealthy()
    {
        var tool = _fixture.Services.GetRequiredService<HealthCheckTool>();
        var input = JsonSerializer.SerializeToElement(new { includeDetails = false });
        var context = new ToolContext { UserId = "test" };
        
        var result = await tool.ExecuteAsync(input, context, CancellationToken.None);
        
        result.Success.Should().BeTrue();
        var output = result.Data as HealthCheckOutput;
        output.Should().NotBeNull();
        output!.Status.Should().Be("healthy");
    }
    
    [Fact]
    public async Task HealthCheck_WithDetails_IncludesComponentStatus()
    {
        var tool = _fixture.Services.GetRequiredService<HealthCheckTool>();
        var input = JsonSerializer.SerializeToElement(new { includeDetails = true });
        var context = new ToolContext { UserId = "test" };
        
        var result = await tool.ExecuteAsync(input, context, CancellationToken.None);
        
        result.Success.Should().BeTrue();
        var output = result.Data as HealthCheckOutput;
        output.Should().NotBeNull();
        output!.AosConnected.Should().BeTrue();
        output.Details.Should().NotBeNull();
        output.Details!["server"].Should().Be("running");
    }
}
