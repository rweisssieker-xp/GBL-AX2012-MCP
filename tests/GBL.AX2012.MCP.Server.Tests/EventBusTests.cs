using System.Threading;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using GBL.AX2012.MCP.Server.Events;

namespace GBL.AX2012.MCP.Server.Tests;

public class EventBusTests
{
    [Fact]
    public async Task Publish_WithSubscriber_CallsHandler()
    {
        // Arrange
        var logger = new Mock<ILogger<EventBus>>();
        var eventBus = new EventBus(logger.Object);
        var handlerCalled = false;
        
        eventBus.Subscribe<SalesOrderCreatedEvent>((evt, ct) =>
        {
            handlerCalled = true;
            return Task.CompletedTask;
        });
        
        var evt = new SalesOrderCreatedEvent
        {
            SalesId = "SO-001",
            CustomerAccount = "CUST-001",
            TotalAmount = 1000,
            CreatedAt = DateTime.UtcNow,
            UserId = "test"
        };
        
        // Act
        await eventBus.PublishAsync(evt);
        
        // Assert
        handlerCalled.Should().BeTrue();
    }
    
    [Fact]
    public async Task Publish_WithoutSubscriber_DoesNotThrow()
    {
        // Arrange
        var logger = new Mock<ILogger<EventBus>>();
        var eventBus = new EventBus(logger.Object);
        
        var evt = new SalesOrderCreatedEvent
        {
            SalesId = "SO-001",
            CustomerAccount = "CUST-001",
            TotalAmount = 1000,
            CreatedAt = DateTime.UtcNow,
            UserId = "test"
        };
        
        // Act & Assert
        await eventBus.Invoking(e => e.PublishAsync(evt))
            .Should().NotThrowAsync();
    }
    
    [Fact]
    public async Task Publish_MultipleSubscribers_CallsAllHandlers()
    {
        // Arrange
        var logger = new Mock<ILogger<EventBus>>();
        var eventBus = new EventBus(logger.Object);
        var callCount = 0;
        
        eventBus.Subscribe<SalesOrderCreatedEvent>((evt, ct) =>
        {
            Interlocked.Increment(ref callCount);
            return Task.CompletedTask;
        });
        
        eventBus.Subscribe<SalesOrderCreatedEvent>((evt, ct) =>
        {
            Interlocked.Increment(ref callCount);
            return Task.CompletedTask;
        });
        
        var evt = new SalesOrderCreatedEvent
        {
            SalesId = "SO-001",
            CustomerAccount = "CUST-001",
            TotalAmount = 1000,
            CreatedAt = DateTime.UtcNow,
            UserId = "test"
        };
        
        // Act
        await eventBus.PublishAsync(evt);
        
        // Assert
        callCount.Should().Be(2);
    }
}

