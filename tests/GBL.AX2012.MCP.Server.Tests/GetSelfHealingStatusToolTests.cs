using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using GBL.AX2012.MCP.Core.Interfaces;
using GBL.AX2012.MCP.Server.Tools;
using GBL.AX2012.MCP.Server.Resilience;

namespace GBL.AX2012.MCP.Server.Tests;

public class GetSelfHealingStatusToolTests
{
    private readonly Mock<ILogger<GetSelfHealingStatusTool>> _logger;
    private readonly Mock<IAuditService> _audit;
    private readonly Mock<ISelfHealingService> _selfHealingService;
    
    public GetSelfHealingStatusToolTests()
    {
        _logger = new Mock<ILogger<GetSelfHealingStatusTool>>();
        _audit = new Mock<IAuditService>();
        _selfHealingService = new Mock<ISelfHealingService>();
    }
    
    [Fact]
    public async Task Execute_ReturnsStatus()
    {
        // Arrange
        var status = new SelfHealingStatus
        {
            CircuitBreakers = new Dictionary<string, ComponentStatus>
            {
                ["aif"] = new ComponentStatus
                {
                    Name = "aif",
                    State = "closed",
                    Status = "healthy",
                    AutoRecoveries = 0
                }
            },
            ConnectionPools = new Dictionary<string, ComponentStatus>
            {
                ["db"] = new ComponentStatus
                {
                    Name = "db",
                    State = "healthy",
                    Status = "healthy",
                    AutoRecoveries = 0
                }
            },
            RetryStats = new RetryStatistics
            {
                TotalRetries = 10,
                SuccessfulRetries = 8,
                FailedRetries = 2
            }
        };
        
        _selfHealingService.Setup(s => s.GetStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(status);
        
        var tool = new GetSelfHealingStatusTool(
            _logger.Object,
            _audit.Object,
            _selfHealingService.Object);
        
        var input = new GetSelfHealingStatusInput();
        var context = new ToolContext { UserId = "test" };
        
        // Act
        var inputJson = JsonSerializer.SerializeToElement(input);
        var result = await tool.ExecuteAsync(inputJson, context, CancellationToken.None);
        
        // Assert
        result.Success.Should().BeTrue();
        var output = JsonSerializer.Deserialize<GetSelfHealingStatusOutput>(result.Data!.ToString()!);
        output.Should().NotBeNull();
        output!.CircuitBreakers.Should().HaveCount(1);
        output.ConnectionPools.Should().HaveCount(1);
        output.RetryStats.TotalRetries.Should().Be(10);
        
        _selfHealingService.Verify(s => s.GetStatusAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

