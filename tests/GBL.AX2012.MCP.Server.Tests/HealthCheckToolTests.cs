using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using FluentAssertions;
using GBL.AX2012.MCP.Core.Interfaces;
using GBL.AX2012.MCP.Core.Models;
using GBL.AX2012.MCP.Core.Options;
using GBL.AX2012.MCP.Server.Tools;
using GBL.AX2012.MCP.AxConnector.Interfaces;

namespace GBL.AX2012.MCP.Server.Tests;

public class HealthCheckToolTests
{
    [Fact]
    public async Task Execute_BasicCheck_ReturnsHealthyStatus()
    {
        var tool = CreateTool();
        var input = JsonSerializer.SerializeToElement(new HealthCheckInput { IncludeDetails = false });
        var context = new ToolContext { UserId = "test" };
        
        var result = await tool.ExecuteAsync(input, context, CancellationToken.None);
        
        result.Success.Should().BeTrue();
        var output = result.Data as HealthCheckOutput;
        output.Should().NotBeNull();
        output!.Status.Should().Be("healthy");
    }
    
    [Fact]
    public async Task Execute_WithDetails_IncludesComponentStatus()
    {
        var businessConnector = new Mock<IBusinessConnector>();
        businessConnector.Setup(x => x.CheckHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AxHealthCheckResult
            {
                Status = "healthy",
                AosConnected = true,
                ResponseTimeMs = 50,
                Details = new Dictionary<string, string>
                {
                    ["company"] = "DAT",
                    ["aos"] = "ax-aos:2712"
                }
            });
        
        var tool = CreateTool(businessConnector.Object);
        var input = JsonSerializer.SerializeToElement(new HealthCheckInput { IncludeDetails = true });
        var context = new ToolContext { UserId = "test" };
        
        var result = await tool.ExecuteAsync(input, context, CancellationToken.None);
        
        result.Success.Should().BeTrue();
        var output = result.Data as HealthCheckOutput;
        output.Should().NotBeNull();
        output!.Details.Should().NotBeNull();
        output.Details!["server"].Should().Be("running");
        output.AosConnected.Should().BeTrue();
    }
    
    [Fact]
    public async Task Execute_AosDisconnected_ReturnsDegradedStatus()
    {
        var businessConnector = new Mock<IBusinessConnector>();
        businessConnector.Setup(x => x.CheckHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AxHealthCheckResult
            {
                Status = "unhealthy",
                AosConnected = false,
                Error = "Connection refused",
                Details = new Dictionary<string, string>()
            });
        
        var tool = CreateTool(businessConnector.Object);
        var input = JsonSerializer.SerializeToElement(new HealthCheckInput { IncludeDetails = true });
        var context = new ToolContext { UserId = "test" };
        
        var result = await tool.ExecuteAsync(input, context, CancellationToken.None);
        
        result.Success.Should().BeTrue();
        var output = result.Data as HealthCheckOutput;
        output.Should().NotBeNull();
        output!.Status.Should().BeOneOf("degraded", "unhealthy");
        output.AosConnected.Should().BeFalse();
    }
    
    private HealthCheckTool CreateTool(IBusinessConnector? businessConnector = null)
    {
        var circuitBreaker = new Mock<ICircuitBreaker>();
        circuitBreaker.Setup(x => x.State).Returns(CircuitState.Closed);
        
        businessConnector ??= CreateMockBusinessConnector();
        
        return new HealthCheckTool(
            Mock.Of<ILogger<HealthCheckTool>>(),
            Mock.Of<IAuditService>(),
            Options.Create(new McpServerOptions { ServerVersion = "1.0.0" }),
            circuitBreaker.Object,
            businessConnector);
    }
    
    private IBusinessConnector CreateMockBusinessConnector()
    {
        var mock = new Mock<IBusinessConnector>();
        mock.Setup(x => x.CheckHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AxHealthCheckResult
            {
                Status = "healthy",
                AosConnected = true,
                ResponseTimeMs = 50,
                Details = new Dictionary<string, string>()
            });
        return mock.Object;
    }
}
