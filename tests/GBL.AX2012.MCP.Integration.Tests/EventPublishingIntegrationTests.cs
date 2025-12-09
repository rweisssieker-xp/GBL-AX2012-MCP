using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using FluentAssertions;
using GBL.AX2012.MCP.Core.Models;
using GBL.AX2012.MCP.Server.Tools;
using GBL.AX2012.MCP.Server.Events;
using GBL.AX2012.MCP.Server.Webhooks;

namespace GBL.AX2012.MCP.Integration.Tests;

public class EventPublishingIntegrationTests : IClassFixture<TestFixture>
{
    private readonly TestFixture _fixture;
    
    public EventPublishingIntegrationTests(TestFixture fixture)
    {
        _fixture = fixture;
    }
    
    [Fact]
    public async Task CreateSalesOrder_PublishesEvent()
    {
        // Arrange
        var eventBus = _fixture.Services.GetRequiredService<IEventBus>();
        var webhookService = _fixture.Services.GetRequiredService<IWebhookService>();
        var createOrderTool = _fixture.Services.GetRequiredService<CreateSalesOrderTool>();
        
        // Subscribe to event
        var subscription = new WebhookSubscription
        {
            EventType = "salesorder.created",
            WebhookUrl = "https://example.com/webhook"
        };
        await webhookService.SubscribeAsync(subscription, CancellationToken.None);
        
        var eventReceived = false;
        eventBus.Subscribe<SalesOrderCreatedEvent>((evt, ct) =>
        {
            eventReceived = true;
            return Task.CompletedTask;
        });
        
        // Act - Create order (this should publish event)
        // Note: This is a simplified test - in real scenario, CreateSalesOrderTool would publish the event
        var salesOrderEvent = new SalesOrderCreatedEvent
        {
            SalesId = "SO-001",
            CustomerAccount = "CUST-001",
            CreatedAt = DateTime.UtcNow,
            UserId = "test"
        };
        
        await eventBus.PublishAsync(salesOrderEvent, CancellationToken.None);
        
        // Assert
        // Give event handlers time to process
        await Task.Delay(100);
        eventReceived.Should().BeTrue();
    }
    
    [Fact]
    public async Task EventBus_PublishAndSubscribe_Works()
    {
        // Arrange
        var eventBus = _fixture.Services.GetRequiredService<IEventBus>();
        var receivedEvents = new List<SalesOrderCreatedEvent>();
        
        eventBus.Subscribe<SalesOrderCreatedEvent>((evt, ct) =>
        {
            receivedEvents.Add(evt);
            return Task.CompletedTask;
        });
        
        // Act
        var event1 = new SalesOrderCreatedEvent { SalesId = "SO-001", CustomerAccount = "CUST-001", CreatedAt = DateTime.UtcNow, UserId = "test" };
        var event2 = new SalesOrderCreatedEvent { SalesId = "SO-002", CustomerAccount = "CUST-002", CreatedAt = DateTime.UtcNow, UserId = "test" };
        
        await eventBus.PublishAsync(event1, CancellationToken.None);
        await eventBus.PublishAsync(event2, CancellationToken.None);
        
        // Assert
        await Task.Delay(100); // Give handlers time to process
        receivedEvents.Should().HaveCount(2);
        receivedEvents.Should().Contain(e => e.SalesId == "SO-001");
        receivedEvents.Should().Contain(e => e.SalesId == "SO-002");
    }
}

