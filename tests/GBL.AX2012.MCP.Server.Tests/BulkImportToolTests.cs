using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using GBL.AX2012.MCP.Core.Interfaces;
using GBL.AX2012.MCP.Server.Tools;

namespace GBL.AX2012.MCP.Server.Tests;

public class BulkImportToolTests
{
    private readonly Mock<ILogger<BulkImportTool>> _logger;
    private readonly Mock<IAuditService> _audit;
    
    public BulkImportToolTests()
    {
        _logger = new Mock<ILogger<BulkImportTool>>();
        _audit = new Mock<IAuditService>();
    }
    
    [Fact]
    public async Task Execute_ValidCsvData_ProcessesSuccessfully()
    {
        // Arrange
        var tool = new BulkImportTool(
            _logger.Object,
            _audit.Object,
            new BulkImportInputValidator());
        
        var csvData = "customer_account,name\nCUST-001,Test Customer 1\nCUST-002,Test Customer 2";
        var input = new BulkImportInput
        {
            Data = csvData,
            Format = "csv",
            EntityType = "customer"
        };
        
        var context = new ToolContext { UserId = "test" };
        
        // Act
        var inputJson = JsonSerializer.SerializeToElement(input);
        var result = await tool.ExecuteAsync(inputJson, context, CancellationToken.None);
        
        // Assert
        result.Success.Should().BeTrue();
        var output = JsonSerializer.Deserialize<BulkImportOutput>(result.Data!.ToString()!);
        output.Should().NotBeNull();
        output!.TotalRecords.Should().BeGreaterThan(0);
    }
    
    [Fact]
    public async Task Execute_InvalidFormat_ReturnsError()
    {
        // Arrange
        var tool = new BulkImportTool(
            _logger.Object,
            _audit.Object,
            new BulkImportInputValidator());
        
        var input = new BulkImportInput
        {
            Data = "invalid data",
            Format = "invalid",
            EntityType = "customer"
        };
        
        var context = new ToolContext { UserId = "test" };
        
        // Act
        var inputJson = JsonSerializer.SerializeToElement(input);
        var result = await tool.ExecuteAsync(inputJson, context, CancellationToken.None);
        
        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().NotBeNullOrEmpty();
    }
}

