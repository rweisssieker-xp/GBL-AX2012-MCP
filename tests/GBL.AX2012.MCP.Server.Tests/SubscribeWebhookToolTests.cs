using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using GBL.AX2012.MCP.Core.Interfaces;
using GBL.AX2012.MCP.Core.Models;
using GBL.AX2012.MCP.Server.Tools;
using GBL.AX2012.MCP.Server.Webhooks;

namespace GBL.AX2012.MCP.Server.Tests;

public class SubscribeWebhookToolTests
{
    private readonly Mock<ILogger<SubscribeWebhookTool>> _logger;
    private readonly Mock<IAuditService> _audit;
    private readonly Mock<IWebhookService> _webhookService;
    
    public SubscribeWebhookToolTests()
    {
        _logger = new Mock<ILogger<SubscribeWebhookTool>>();
        _audit = new Mock<IAuditService>();
        _webhookService = new Mock<IWebhookService>();
    }
    
    [Fact]
    public async Task Execute_ValidSubscription_CreatesWebhook()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var subscription = new WebhookSubscription
        {
            Id = subscriptionId,
            EventType = "salesorder.created",
            WebhookUrl = "https://example.com/webhook",
            CreatedAt = DateTime.UtcNow
        };
        
        _webhookService.Setup(s => s.SubscribeAsync(It.IsAny<WebhookSubscription>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        
        var tool = new SubscribeWebhookTool(
            _logger.Object,
            _audit.Object,
            new SubscribeWebhookInputValidator(),
            _webhookService.Object);
        
        var input = new SubscribeWebhookInput
        {
            EventType = "salesorder.created",
            WebhookUrl = "https://example.com/webhook"
        };
        
        var context = new ToolContext { UserId = "test" };
        
        // Act
        var inputJson = JsonSerializer.SerializeToElement(input);
        var result = await tool.ExecuteAsync(inputJson, context, CancellationToken.None);
        
        // Assert
        result.Success.Should().BeTrue();
        var output = JsonSerializer.Deserialize<SubscribeWebhookOutput>(result.Data!.ToString()!);
        output.Should().NotBeNull();
        output!.SubscriptionId.Should().Be(subscriptionId);
        output.EventType.Should().Be("salesorder.created");
        output.WebhookUrl.Should().Be("https://example.com/webhook");
        
        _webhookService.Verify(s => s.SubscribeAsync(It.IsAny<WebhookSubscription>(), It.IsAny<CancellationToken>()), Times.Once);
    }
    
    [Fact]
    public async Task Execute_InvalidEventType_ReturnsError()
    {
        // Arrange
        var tool = new SubscribeWebhookTool(
            _logger.Object,
            _audit.Object,
            new SubscribeWebhookInputValidator(),
            _webhookService.Object);
        
        var input = new SubscribeWebhookInput
        {
            EventType = "invalid.event",
            WebhookUrl = "https://example.com/webhook"
        };
        
        var context = new ToolContext { UserId = "test" };
        
        // Act
        var inputJson = JsonSerializer.SerializeToElement(input);
        var result = await tool.ExecuteAsync(inputJson, context, CancellationToken.None);
        
        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().NotBeNullOrEmpty();
    }
    
    [Fact]
    public async Task Execute_WithRetryPolicy_UsesCustomPolicy()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var subscription = new WebhookSubscription
        {
            Id = subscriptionId,
            EventType = "salesorder.created",
            WebhookUrl = "https://example.com/webhook",
            RetryPolicy = new WebhookRetryPolicy
            {
                MaxRetries = 5,
                BackoffMs = 2000,
                ExponentialBackoff = false
            },
            CreatedAt = DateTime.UtcNow
        };
        
        _webhookService.Setup(s => s.SubscribeAsync(It.Is<WebhookSubscription>(w => 
            w.RetryPolicy.MaxRetries == 5 && 
            w.RetryPolicy.BackoffMs == 2000 &&
            !w.RetryPolicy.ExponentialBackoff), It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        
        var tool = new SubscribeWebhookTool(
            _logger.Object,
            _audit.Object,
            new SubscribeWebhookInputValidator(),
            _webhookService.Object);
        
        var input = new SubscribeWebhookInput
        {
            EventType = "salesorder.created",
            WebhookUrl = "https://example.com/webhook",
            RetryPolicy = new WebhookRetryPolicyInput
            {
                MaxRetries = 5,
                BackoffMs = 2000,
                ExponentialBackoff = false
            }
        };
        
        var context = new ToolContext { UserId = "test" };
        
        // Act
        var inputJson = JsonSerializer.SerializeToElement(input);
        var result = await tool.ExecuteAsync(inputJson, context, CancellationToken.None);
        
        // Assert
        result.Success.Should().BeTrue();
        _webhookService.Verify(s => s.SubscribeAsync(It.Is<WebhookSubscription>(w => 
            w.RetryPolicy.MaxRetries == 5), It.IsAny<CancellationToken>()), Times.Once);
    }
}

