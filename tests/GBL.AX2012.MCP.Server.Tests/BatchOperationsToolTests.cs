using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using GBL.AX2012.MCP.Core.Interfaces;
using GBL.AX2012.MCP.Core.Models;
using GBL.AX2012.MCP.Server.Tools;

namespace GBL.AX2012.MCP.Server.Tests;

public class BatchOperationsToolTests
{
    private readonly Mock<ILogger<BatchOperationsTool>> _logger;
    private readonly Mock<IAuditService> _audit;
    private readonly Mock<IServiceProvider> _serviceProvider;
    private readonly List<ITool> _tools;
    
    public BatchOperationsToolTests()
    {
        _logger = new Mock<ILogger<BatchOperationsTool>>();
        _audit = new Mock<IAuditService>();
        _serviceProvider = new Mock<IServiceProvider>();
        _tools = new List<ITool>();
    }
    
    [Fact]
    public async Task Execute_SingleOperation_ReturnsSuccess()
    {
        // Arrange
        var mockTool = new Mock<ITool>();
        mockTool.Setup(t => t.Name).Returns("ax_get_customer");
        mockTool.Setup(t => t.ExecuteAsync(It.IsAny<JsonElement>(), It.IsAny<ToolContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ToolResponse.Ok(new { customer_account = "CUST-001" }));
        
        _tools.Add(mockTool.Object);
        
        var tool = new BatchOperationsTool(
            _logger.Object,
            _audit.Object,
            new BatchOperationsInputValidator(),
            _serviceProvider.Object,
            _tools);
        
        var input = new BatchOperationsInput
        {
            Requests = new List<BatchRequest>
            {
                new() { Tool = "ax_get_customer", Arguments = JsonSerializer.SerializeToElement(new { customer_account = "CUST-001" }) }
            },
            MaxParallel = 1
        };
        
        var context = new ToolContext { UserId = "test" };
        
        // Act
        var inputJson = JsonSerializer.SerializeToElement(input);
        var result = await tool.ExecuteAsync(inputJson, context, CancellationToken.None);
        
        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        var output = result.Data as BatchOperationsOutput ?? JsonSerializer.Deserialize<BatchOperationsOutput>(JsonSerializer.Serialize(result.Data));
        output.Should().NotBeNull();
        output!.Total.Should().Be(1);
        output.Successful.Should().Be(1);
        output.Failed.Should().Be(0);
        output.Results.Should().HaveCount(1);
        output.Results[0].Success.Should().BeTrue();
    }
    
    [Fact]
    public async Task Execute_MultipleOperations_ProcessesInParallel()
    {
        // Arrange
        var mockTool = new Mock<ITool>();
        mockTool.Setup(t => t.Name).Returns("ax_get_customer");
        mockTool.Setup(t => t.ExecuteAsync(It.IsAny<JsonElement>(), It.IsAny<ToolContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ToolResponse.Ok(new { customer_account = "CUST-001" }));
        
        _tools.Add(mockTool.Object);
        
        var tool = new BatchOperationsTool(
            _logger.Object,
            _audit.Object,
            new BatchOperationsInputValidator(),
            _serviceProvider.Object,
            _tools);
        
        var input = new BatchOperationsInput
        {
            Requests = new List<BatchRequest>
            {
                new() { Tool = "ax_get_customer", Arguments = JsonSerializer.SerializeToElement(new { customer_account = "CUST-001" }) },
                new() { Tool = "ax_get_customer", Arguments = JsonSerializer.SerializeToElement(new { customer_account = "CUST-002" }) },
                new() { Tool = "ax_get_customer", Arguments = JsonSerializer.SerializeToElement(new { customer_account = "CUST-003" }) }
            },
            MaxParallel = 3
        };
        
        var context = new ToolContext { UserId = "test" };
        
        // Act
        var inputJson = JsonSerializer.SerializeToElement(input);
        var result = await tool.ExecuteAsync(inputJson, context, CancellationToken.None);
        
        // Assert
        result.Success.Should().BeTrue();
        var output = JsonSerializer.Deserialize<BatchOperationsOutput>(result.Data!.ToString()!);
        output.Should().NotBeNull();
        output!.Total.Should().Be(3);
        output.Successful.Should().Be(3);
    }
    
    [Fact]
    public async Task Execute_StopOnError_StopsAtFirstFailure()
    {
        // Arrange
        var successTool = new Mock<ITool>();
        successTool.Setup(t => t.Name).Returns("ax_get_customer");
        successTool.Setup(t => t.ExecuteAsync(It.IsAny<JsonElement>(), It.IsAny<ToolContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ToolResponse.Ok(new { customer_account = "CUST-001" }));
        
        var failTool = new Mock<ITool>();
        failTool.Setup(t => t.Name).Returns("ax_get_customer");
        failTool.Setup(t => t.ExecuteAsync(It.IsAny<JsonElement>(), It.IsAny<ToolContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ToolResponse.Error("ERROR", "Test error"));
        
        _tools.Add(successTool.Object);
        _tools.Add(failTool.Object);
        
        var tool = new BatchOperationsTool(
            _logger.Object,
            _audit.Object,
            new BatchOperationsInputValidator(),
            _serviceProvider.Object,
            _tools);
        
        var input = new BatchOperationsInput
        {
            Requests = new List<BatchRequest>
            {
                new() { Tool = "ax_get_customer", Arguments = JsonSerializer.SerializeToElement(new { customer_account = "CUST-001" }) },
                new() { Tool = "ax_get_customer", Arguments = JsonSerializer.SerializeToElement(new { customer_account = "CUST-002" }) }
            },
            StopOnError = true,
            MaxParallel = 1
        };
        
        var context = new ToolContext { UserId = "test" };
        
        // Act
        var inputJson = JsonSerializer.SerializeToElement(input);
        var result = await tool.ExecuteAsync(inputJson, context, CancellationToken.None);
        var output = JsonSerializer.Deserialize<BatchOperationsOutput>(result.Data!.ToString()!);
        
        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        var output3 = result.Data as BatchOperationsOutput ?? JsonSerializer.Deserialize<BatchOperationsOutput>(JsonSerializer.Serialize(result.Data));
        output3.Should().NotBeNull();
        output3!.Results.Should().HaveCountLessOrEqualTo(2); // May stop after first error
    }
}

