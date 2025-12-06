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

public class GetCustomerToolTests
{
    [Fact]
    public async Task Execute_ByAccount_ReturnsCustomer()
    {
        var aifClient = new Mock<IAifClient>();
        aifClient.Setup(x => x.GetCustomerAsync("CUST-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Customer
            {
                AccountNum = "CUST-001",
                Name = "Test Customer",
                Currency = "EUR",
                CreditLimit = 100000,
                CreditUsed = 25000,
                PaymentTerms = "Net30",
                PriceGroup = "RETAIL"
            });
        
        var tool = CreateTool(aifClient.Object);
        var input = JsonSerializer.SerializeToElement(new GetCustomerInput { CustomerAccount = "CUST-001" });
        var context = new ToolContext { UserId = "test" };
        
        var result = await tool.ExecuteAsync(input, context, CancellationToken.None);
        
        result.Success.Should().BeTrue();
        var output = result.Data as GetCustomerOutput;
        output.Should().NotBeNull();
        output!.CustomerAccount.Should().Be("CUST-001");
        output.Name.Should().Be("Test Customer");
        output.CreditAvailable.Should().Be(75000);
    }
    
    [Fact]
    public async Task Execute_ByName_ReturnsSearchResults()
    {
        var aifClient = new Mock<IAifClient>();
        aifClient.Setup(x => x.SearchCustomersAsync("Test", 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new Customer { AccountNum = "CUST-001", Name = "Test Customer", MatchConfidence = 95 },
                new Customer { AccountNum = "CUST-002", Name = "Testing Corp", MatchConfidence = 80 }
            });
        
        var tool = CreateTool(aifClient.Object);
        var input = JsonSerializer.SerializeToElement(new GetCustomerInput { CustomerName = "Test" });
        var context = new ToolContext { UserId = "test" };
        
        var result = await tool.ExecuteAsync(input, context, CancellationToken.None);
        
        result.Success.Should().BeTrue();
        var output = result.Data as CustomerSearchOutput;
        output.Should().NotBeNull();
        output!.Matches.Should().HaveCount(2);
        output.Matches[0].CustomerAccount.Should().Be("CUST-001");
        output.Matches[0].Confidence.Should().Be(95);
    }
    
    [Fact]
    public async Task Execute_CustomerNotFound_ReturnsError()
    {
        var aifClient = new Mock<IAifClient>();
        aifClient.Setup(x => x.GetCustomerAsync("INVALID", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);
        
        var tool = CreateTool(aifClient.Object);
        var input = JsonSerializer.SerializeToElement(new GetCustomerInput { CustomerAccount = "INVALID" });
        var context = new ToolContext { UserId = "test" };
        
        var result = await tool.ExecuteAsync(input, context, CancellationToken.None);
        
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("CUST_NOT_FOUND");
    }
    
    [Fact]
    public async Task Execute_NoInput_ReturnsValidationError()
    {
        var aifClient = new Mock<IAifClient>();
        var tool = CreateTool(aifClient.Object);
        var input = JsonSerializer.SerializeToElement(new GetCustomerInput { });
        var context = new ToolContext { UserId = "test" };
        
        var result = await tool.ExecuteAsync(input, context, CancellationToken.None);
        
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("VALIDATION_ERROR");
    }
    
    private GetCustomerTool CreateTool(IAifClient aifClient)
    {
        return new GetCustomerTool(
            Mock.Of<ILogger<GetCustomerTool>>(),
            Mock.Of<IAuditService>(),
            new GetCustomerInputValidator(),
            aifClient);
    }
}
