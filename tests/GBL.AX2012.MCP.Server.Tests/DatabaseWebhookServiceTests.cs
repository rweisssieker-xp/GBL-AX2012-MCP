using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Xunit;
using FluentAssertions;
using GBL.AX2012.MCP.Server.Events;
using GBL.AX2012.MCP.Server.Webhooks;
using GBL.AX2012.MCP.Audit.Data;
using GBL.AX2012.MCP.Core.Models;
using GBL.AX2012.MCP.Core.Interfaces;

namespace GBL.AX2012.MCP.Server.Tests;

public class DatabaseWebhookServiceTests : IDisposable
{
    private readonly WebhookDbContext _context;
    private readonly Mock<ILogger<DatabaseWebhookService>> _logger;
    private readonly Mock<IEventBus> _eventBus;
    private readonly Mock<HttpMessageHandler> _httpHandler;
    private readonly HttpClient _httpClient;
    private readonly DatabaseWebhookService _service;
    private readonly IDbContextFactory<WebhookDbContext> _contextFactory;
    
    public DatabaseWebhookServiceTests()
    {
        var databaseName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<WebhookDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName)
            .Options;
        
        _context = new WebhookDbContext(options);
        _logger = new Mock<ILogger<DatabaseWebhookService>>();
        _eventBus = new Mock<IEventBus>();
        _httpHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpHandler.Object);
        
        // Create a real factory that returns new contexts sharing the same in-memory database
        _contextFactory = new TestDbContextFactory(_context, databaseName);
        
        var webhookOptions = Options.Create(new WebhookServiceOptions
        {
            MaxConcurrentDeliveries = 10,
            DeliveryTimeoutSeconds = 30
        });
        
        _service = new DatabaseWebhookService(
            _logger.Object,
            _httpClient,
            _eventBus.Object,
            _contextFactory,
            webhookOptions);
    }
    
    [Fact]
    public async Task SubscribeAsync_CreatesSubscription()
    {
        // Arrange
        var subscription = new WebhookSubscription
        {
            EventType = "salesorder.created",
            WebhookUrl = "https://example.com/webhook"
        };
        
        // Act
        var result = await _service.SubscribeAsync(subscription, CancellationToken.None);
        
        // Assert
        result.Id.Should().NotBeEmpty();
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        
        // Use a new context to verify the subscription was saved
        await using var verifyContext = await _contextFactory.CreateDbContextAsync(CancellationToken.None);
        var dbSubscription = await verifyContext.WebhookSubscriptions.FindAsync(result.Id);
        dbSubscription.Should().NotBeNull();
        dbSubscription!.EventType.Should().Be("salesorder.created");
    }
    
    [Fact]
    public async Task ListSubscriptionsAsync_ReturnsActiveSubscriptions()
    {
        // Arrange
        var subscription1 = new WebhookSubscription
        {
            Id = Guid.NewGuid(),
            EventType = "salesorder.created",
            WebhookUrl = "https://example.com/webhook1",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        
        var subscription2 = new WebhookSubscription
        {
            Id = Guid.NewGuid(),
            EventType = "payment.posted",
            WebhookUrl = "https://example.com/webhook2",
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        };
        
        _context.WebhookSubscriptions.Add(WebhookSubscriptionEntity.FromModel(subscription1));
        _context.WebhookSubscriptions.Add(WebhookSubscriptionEntity.FromModel(subscription2));
        await _context.SaveChangesAsync();
        
        // Act
        var result = await _service.ListSubscriptionsAsync(null, CancellationToken.None);
        
        // Assert
        result.Should().HaveCount(1);
        result[0].EventType.Should().Be("salesorder.created");
    }
    
    [Fact]
    public async Task UnsubscribeAsync_DeactivatesSubscription()
    {
        // Arrange
        var subscription = new WebhookSubscription
        {
            Id = Guid.NewGuid(),
            EventType = "salesorder.created",
            WebhookUrl = "https://example.com/webhook",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        
        _context.WebhookSubscriptions.Add(WebhookSubscriptionEntity.FromModel(subscription));
        await _context.SaveChangesAsync();
        
        // Act
        await _service.UnsubscribeAsync(subscription.Id, CancellationToken.None);
        
        // Assert
        var dbSubscription = await _context.WebhookSubscriptions.FindAsync(subscription.Id);
        dbSubscription.Should().NotBeNull();
        dbSubscription!.IsActive.Should().BeFalse();
    }
    
    [Fact]
    public async Task TriggerWebhookAsync_DeliversWebhook()
    {
        // Arrange
        var subscription = new WebhookSubscription
        {
            Id = Guid.NewGuid(),
            EventType = "salesorder.created",
            WebhookUrl = "https://example.com/webhook",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        
        _context.WebhookSubscriptions.Add(WebhookSubscriptionEntity.FromModel(subscription));
        await _context.SaveChangesAsync();
        
        _httpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("OK")
            });
        
        // Act
        await _service.TriggerWebhookAsync("salesorder.created", new { orderId = "123" }, CancellationToken.None);
        
        // Assert
        _httpHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString() == "https://example.com/webhook"),
            ItExpr.IsAny<CancellationToken>());
    }
    
    public void Dispose()
    {
        _context?.Dispose();
        _httpClient?.Dispose();
    }
}

