using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using GBL.AX2012.MCP.Core.Interfaces;
using GBL.AX2012.MCP.Core.Models;
using GBL.AX2012.MCP.Server.Tools;
using GBL.AX2012.MCP.AxConnector.Interfaces;

namespace GBL.AX2012.MCP.Server.Tests;

public class BulkImportToolTests
{
    private readonly Mock<ILogger<BulkImportTool>> _logger;
    private readonly Mock<IAuditService> _audit;
    private readonly Mock<IAifClient> _aifClient;
    private readonly Mock<IWcfClient> _wcfClient;
    
    public BulkImportToolTests()
    {
        _logger = new Mock<ILogger<BulkImportTool>>();
        _audit = new Mock<IAuditService>();
        _aifClient = new Mock<IAifClient>();
        _wcfClient = new Mock<IWcfClient>();
    }
    
    [Fact]
    public async Task Execute_ValidCsvData_ProcessesSuccessfully()
    {
        // Arrange
        var tool = new BulkImportTool(
            _logger.Object,
            _audit.Object,
            new BulkImportInputValidator(),
            _aifClient.Object,
            _wcfClient.Object);
        
        var csvData = "customer_account,name\nCUST-001,Test Customer 1\nCUST-002,Test Customer 2";
        var input = new BulkImportInput
        {
            Data = csvData,
            Format = "csv",
            Type = "customers"
        };
        
        var context = new ToolContext { UserId = "test" };
        
        // Act
        var inputJson = JsonSerializer.SerializeToElement(input);
        var result = await tool.ExecuteAsync(inputJson, context, CancellationToken.None);
        
        // Assert
        result.Success.Should().BeTrue();
        var output = JsonSerializer.Deserialize<BulkImportOutput>(result.Data!.ToString()!);
        output.Should().NotBeNull();
        output!.Total.Should().BeGreaterThan(0);
    }
    
    [Fact]
    public async Task Execute_InvalidFormat_ReturnsError()
    {
        // Arrange
        var tool = new BulkImportTool(
            _logger.Object,
            _audit.Object,
            new BulkImportInputValidator(),
            _aifClient.Object,
            _wcfClient.Object);
        
        var input = new BulkImportInput
        {
            Data = "invalid data",
            Format = "invalid",
            Type = "customers"
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

