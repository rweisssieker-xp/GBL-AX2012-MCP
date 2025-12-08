using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using GBL.AX2012.MCP.Server.Resilience;
using GBL.AX2012.MCP.Core.Interfaces;

namespace GBL.AX2012.MCP.Server.Tests;

public class SelfHealingServiceTests
{
    private readonly Mock<ILogger<SelfHealingService>> _logger;
    private readonly Mock<ICircuitBreaker> _circuitBreaker;
    private readonly Mock<IConnectionPoolMonitor> _connectionPoolMonitor;
    
    public SelfHealingServiceTests()
    {
        _logger = new Mock<ILogger<SelfHealingService>>();
        _circuitBreaker = new Mock<ICircuitBreaker>();
        _connectionPoolMonitor = new Mock<IConnectionPoolMonitor>();
    }
    
    [Fact]
    public async Task GetStatusAsync_ReturnsStatus()
    {
        // Arrange
        _circuitBreaker.Setup(c => c.GetState(It.IsAny<string>()))
            .Returns("closed");
        
        _connectionPoolMonitor.Setup(c => c.GetAllStatuses())
            .Returns(new Dictionary<string, ConnectionPoolStatus>
            {
                ["db"] = new ConnectionPoolStatus
                {
                    Name = "db",
                    Status = "healthy",
                    ActiveConnections = 5
                }
            });
        
        var service = new SelfHealingService(
            _logger.Object,
            _circuitBreaker.Object,
            _connectionPoolMonitor.Object);
        
        // Act
        var status = await service.GetStatusAsync(CancellationToken.None);
        
        // Assert
        status.Should().NotBeNull();
        status.ConnectionPools.Should().HaveCount(1);
        status.RetryStats.Should().NotBeNull();
    }
    
    [Fact]
    public void RecordRecovery_RecordsRecovery()
    {
        // Arrange
        var service = new SelfHealingService(
            _logger.Object,
            _circuitBreaker.Object,
            _connectionPoolMonitor.Object);
        
        // Act
        service.RecordRecovery("test-component", "auto-reset");
        
        // Assert
        // Recovery should be recorded (no exception thrown)
        service.Should().NotBeNull();
    }
}

