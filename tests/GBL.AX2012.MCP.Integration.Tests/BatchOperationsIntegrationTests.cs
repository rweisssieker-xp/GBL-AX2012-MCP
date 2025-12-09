using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using FluentAssertions;
using GBL.AX2012.MCP.Core.Models;
using GBL.AX2012.MCP.Server.Tools;

namespace GBL.AX2012.MCP.Integration.Tests;

public class BatchOperationsIntegrationTests : IClassFixture<TestFixture>
{
    private readonly TestFixture _fixture;
    
    public BatchOperationsIntegrationTests(TestFixture fixture)
    {
        _fixture = fixture;
    }
    
    [Fact]
    public async Task BatchOperations_MultipleReadOperations_ProcessesSuccessfully()
    {
        // Arrange
        var tool = _fixture.Services.GetRequiredService<BatchOperationsTool>();
        var input = new BatchOperationsInput
        {
            Requests = new List<BatchRequest>
            {
                new() { Tool = "ax_get_customer", Arguments = JsonSerializer.SerializeToElement(new { customer_account = "CUST-001" }) },
                new() { Tool = "ax_get_customer", Arguments = JsonSerializer.SerializeToElement(new { customer_account = "CUST-002" }) }
            },
            MaxParallel = 2
        };
        
        var context = new ToolContext { UserId = "test" };
        
        // Act
        var inputJson = JsonSerializer.SerializeToElement(input);
        var result = await tool.ExecuteAsync(inputJson, context, CancellationToken.None);
        
        // Assert
        result.Success.Should().BeTrue();
        var output = JsonSerializer.Deserialize<BatchOperationsOutput>(result.Data!.ToString()!);
        output.Should().NotBeNull();
        output!.Total.Should().Be(2);
    }
    
    [Fact]
    public async Task BatchOperations_WithError_ContinuesProcessing()
    {
        // Arrange
        var tool = _fixture.Services.GetRequiredService<BatchOperationsTool>();
        var input = new BatchOperationsInput
        {
            Requests = new List<BatchRequest>
            {
                new() { Tool = "ax_get_customer", Arguments = JsonSerializer.SerializeToElement(new { customer_account = "VALID" }) },
                new() { Tool = "ax_get_customer", Arguments = JsonSerializer.SerializeToElement(new { customer_account = "INVALID" }) }
            },
            StopOnError = false,
            MaxParallel = 2
        };
        
        var context = new ToolContext { UserId = "test" };
        
        // Act
        var inputJson = JsonSerializer.SerializeToElement(input);
        var result = await tool.ExecuteAsync(inputJson, context, CancellationToken.None);
        
        // Assert
        result.Success.Should().BeTrue();
        var output = JsonSerializer.Deserialize<BatchOperationsOutput>(result.Data!.ToString()!);
        output.Should().NotBeNull();
        output!.Total.Should().Be(2);
    }
}

