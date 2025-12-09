using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using GBL.AX2012.MCP.Server.Resilience;

namespace GBL.AX2012.MCP.Server.Tests;

public class ConnectionPoolMonitorTests
{
    private readonly Mock<ILogger<ConnectionPoolMonitor>> _logger;
    private readonly Mock<ISelfHealingService> _selfHealingService;
    
    public ConnectionPoolMonitorTests()
    {
        _logger = new Mock<ILogger<ConnectionPoolMonitor>>();
        _selfHealingService = new Mock<ISelfHealingService>();
    }
    
    [Fact]
    public void RecordConnectionSuccess_UpdatesStatus()
    {
        // Arrange
        var monitor = new ConnectionPoolMonitor(
            _logger.Object,
            _selfHealingService.Object);
        
        // Act
        monitor.RecordConnectionSuccess("test-pool");
        
        // Assert
        var status = monitor.GetStatus("test-pool");
        status.Should().NotBeNull();
        status.Name.Should().Be("test-pool");
    }
    
    [Fact]
    public void RecordConnectionFailure_UpdatesFailureCount()
    {
        // Arrange
        var monitor = new ConnectionPoolMonitor(
            _logger.Object,
            _selfHealingService.Object);
        
        // Act
        monitor.RecordConnectionFailure("test-pool");
        monitor.RecordConnectionFailure("test-pool");
        
        // Assert
        var status = monitor.GetStatus("test-pool");
        status.Should().NotBeNull();
        status.FailedConnections.Should().BeGreaterThan(0);
    }
    
    [Fact]
    public void GetStatus_ReturnsPoolStatus()
    {
        // Arrange
        var monitor = new ConnectionPoolMonitor(
            _logger.Object,
            _selfHealingService.Object);
        
        monitor.RecordConnectionSuccess("test-pool");
        
        // Act
        var status = monitor.GetStatus("test-pool");
        
        // Assert
        status.Should().NotBeNull();
        status.Name.Should().Be("test-pool");
        status.Status.Should().Be("healthy");
    }
    
    [Fact]
    public void GetAllStatuses_ReturnsAllPools()
    {
        // Arrange
        var monitor = new ConnectionPoolMonitor(
            _logger.Object,
            _selfHealingService.Object);
        
        monitor.RecordConnectionSuccess("pool1");
        monitor.RecordConnectionSuccess("pool2");
        
        // Act
        var statuses = monitor.GetAllStatuses();
        
        // Assert
        statuses.Should().HaveCount(2);
        statuses.Should().ContainKey("pool1");
        statuses.Should().ContainKey("pool2");
    }
}

