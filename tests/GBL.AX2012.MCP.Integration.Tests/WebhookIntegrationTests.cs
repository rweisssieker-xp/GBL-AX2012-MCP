using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using FluentAssertions;
using GBL.AX2012.MCP.Core.Models;
using GBL.AX2012.MCP.Server.Tools;

namespace GBL.AX2012.MCP.Integration.Tests;

public class WebhookIntegrationTests : IClassFixture<TestFixture>
{
    private readonly TestFixture _fixture;
    
    public WebhookIntegrationTests(TestFixture fixture)
    {
        _fixture = fixture;
    }
    
    [Fact]
    public async Task Webhook_SubscribeAndList_WorksEndToEnd()
    {
        // Arrange
        var subscribeTool = _fixture.Services.GetRequiredService<SubscribeWebhookTool>();
        var listTool = _fixture.Services.GetRequiredService<ListWebhooksTool>();
        
        var subscribeInput = new SubscribeWebhookInput
        {
            EventType = "salesorder.created",
            WebhookUrl = "https://example.com/webhook"
        };
        
        var context = new ToolContext { UserId = "test" };
        
        // Act - Subscribe
        var subscribeInputJson = JsonSerializer.SerializeToElement(subscribeInput);
        var subscribeResult = await subscribeTool.ExecuteAsync(subscribeInputJson, context, CancellationToken.None);
        
        // Assert - Subscribe
        subscribeResult.Success.Should().BeTrue();
        subscribeResult.Data.Should().NotBeNull();
        var subscribeOutput = subscribeResult.Data as SubscribeWebhookOutput ?? JsonSerializer.Deserialize<SubscribeWebhookOutput>(JsonSerializer.Serialize(subscribeResult.Data));
        subscribeOutput.Should().NotBeNull();
        subscribeOutput!.SubscriptionId.Should().NotBeEmpty();
        
        // Act - List
        var listInput = new ListWebhooksInput { EventType = "salesorder.created" };
        var listInputJson = JsonSerializer.SerializeToElement(listInput);
        var listResult = await listTool.ExecuteAsync(listInputJson, context, CancellationToken.None);
        
        // Assert - List
        listResult.Success.Should().BeTrue();
        listResult.Data.Should().NotBeNull();
        var listOutput = listResult.Data as ListWebhooksOutput ?? JsonSerializer.Deserialize<ListWebhooksOutput>(JsonSerializer.Serialize(listResult.Data));
        listOutput.Should().NotBeNull();
        listOutput!.Subscriptions.Should().Contain(s => s.SubscriptionId == subscribeOutput.SubscriptionId);
    }
    
    [Fact]
    public async Task Webhook_SubscribeAndUnsubscribe_WorksEndToEnd()
    {
        // Arrange
        var subscribeTool = _fixture.Services.GetRequiredService<SubscribeWebhookTool>();
        var unsubscribeTool = _fixture.Services.GetRequiredService<UnsubscribeWebhookTool>();
        
        var subscribeInput = new SubscribeWebhookInput
        {
            EventType = "payment.posted",
            WebhookUrl = "https://example.com/webhook"
        };
        
        var context = new ToolContext { UserId = "test" };
        
        // Act - Subscribe
        var subscribeInputJson = JsonSerializer.SerializeToElement(subscribeInput);
        var subscribeResult = await subscribeTool.ExecuteAsync(subscribeInputJson, context, CancellationToken.None);
        var subscribeOutput = subscribeResult.Data as SubscribeWebhookOutput ?? JsonSerializer.Deserialize<SubscribeWebhookOutput>(JsonSerializer.Serialize(subscribeResult.Data));
        
        // Act - Unsubscribe
        var unsubscribeInput = new UnsubscribeWebhookInput
        {
            SubscriptionId = subscribeOutput!.SubscriptionId
        };
        var unsubscribeInputJson = JsonSerializer.SerializeToElement(unsubscribeInput);
        var unsubscribeResult = await unsubscribeTool.ExecuteAsync(unsubscribeInputJson, context, CancellationToken.None);
        
        // Assert
        unsubscribeResult.Success.Should().BeTrue();
        var unsubscribeOutput = JsonSerializer.Deserialize<UnsubscribeWebhookOutput>(unsubscribeResult.Data!.ToString()!);
        unsubscribeOutput.Should().NotBeNull();
        unsubscribeOutput!.Success.Should().BeTrue();
    }
}

