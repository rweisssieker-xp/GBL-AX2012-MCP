using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using GBL.AX2012.MCP.Core.Interfaces;
using GBL.AX2012.MCP.Core.Models;
using GBL.AX2012.MCP.Server.Tools;

namespace GBL.AX2012.MCP.Server.Tests;

public class GetRoiMetricsToolTests
{
    private readonly Mock<ILogger<GetRoiMetricsTool>> _logger;
    private readonly Mock<IAuditService> _auditService;
    
    public GetRoiMetricsToolTests()
    {
        _logger = new Mock<ILogger<GetRoiMetricsTool>>();
        _auditService = new Mock<IAuditService>();
    }
    
    [Fact]
    public async Task Execute_WithAuditData_CalculatesMetrics()
    {
        // Arrange
        var auditEntries = new List<AuditEntry>
        {
            new()
            {
                ToolName = "ax_create_salesorder",
                Success = true,
                DurationMs = 1500,
                Timestamp = DateTime.UtcNow.AddDays(-1),
                UserId = "user1"
            },
            new()
            {
                ToolName = "ax_create_salesorder",
                Success = true,
                DurationMs = 1200,
                Timestamp = DateTime.UtcNow.AddDays(-1),
                UserId = "user1"
            }
        };
        
        _auditService.Setup(a => a.QueryAsync(It.IsAny<AuditQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(auditEntries);
        
        var tool = new GetRoiMetricsTool(
            _logger.Object,
            _auditService.Object,
            new GetRoiMetricsInputValidator());
        
        var input = new GetRoiMetricsInput
        {
            DateFrom = DateTime.UtcNow.AddDays(-30),
            DateTo = DateTime.UtcNow
        };
        
        var context = new ToolContext { UserId = "test" };
        
        // Act
        var inputJson = JsonSerializer.SerializeToElement(input);
        var result = await tool.ExecuteAsync(inputJson, context, CancellationToken.None);
        
        // Assert
        result.Success.Should().BeTrue();
        var output = JsonSerializer.Deserialize<GetRoiMetricsOutput>(result.Data!.ToString()!);
        output.Should().NotBeNull();
        output!.TotalOperations.Should().BeGreaterThan(0);
        output.ByTool.Should().NotBeEmpty();
    }
}

