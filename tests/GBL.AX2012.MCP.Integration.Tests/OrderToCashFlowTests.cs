using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using FluentAssertions;
using GBL.AX2012.MCP.Core.Models;
using GBL.AX2012.MCP.Server.Tools;

namespace GBL.AX2012.MCP.Integration.Tests;

public class OrderToCashFlowTests : IClassFixture<TestFixture>
{
    private readonly TestFixture _fixture;
    
    public OrderToCashFlowTests(TestFixture fixture)
    {
        _fixture = fixture;
    }
    
    [Fact]
    public async Task FullOrderFlow_FromCustomerLookup_ToOrderCreation()
    {
        // Arrange
        var context = new ToolContext
        {
            UserId = "test-user",
            Roles = new[] { "MCP_Read", "MCP_Write" },
            CorrelationId = Guid.NewGuid().ToString()
        };
        
        // Step 1: Get Customer
        var getCustomerTool = _fixture.Services.GetRequiredService<GetCustomerTool>();
        var customerInput = JsonSerializer.SerializeToElement(new { customerAccount = "CUST-001" });
        
        var customerResult = await getCustomerTool.ExecuteAsync(customerInput, context, CancellationToken.None);
        
        customerResult.Success.Should().BeTrue();
        var customer = customerResult.Data as GetCustomerOutput;
        customer.Should().NotBeNull();
        customer!.CustomerAccount.Should().Be("CUST-001");
        customer.Name.Should().Be("Müller GmbH");
        customer.CreditAvailable.Should().Be(75000);
        
        // Step 2: Check Credit
        var checkCreditTool = _fixture.Services.GetRequiredService<CheckCreditTool>();
        var creditInput = JsonSerializer.SerializeToElement(new 
        { 
            customerAccount = "CUST-001",
            proposedAmount = 10000
        });
        
        var creditResult = await checkCreditTool.ExecuteAsync(creditInput, context, CancellationToken.None);
        
        creditResult.Success.Should().BeTrue();
        var credit = creditResult.Data as CheckCreditOutput;
        credit.Should().NotBeNull();
        credit!.WouldExceedLimit.Should().BeFalse();
        credit.Recommendation.Should().Contain("APPROVE");
        
        // Step 3: Check Inventory
        var checkInventoryTool = _fixture.Services.GetRequiredService<CheckInventoryTool>();
        var inventoryInput = JsonSerializer.SerializeToElement(new 
        { 
            itemId = "ITEM-100",
            warehouse = "WH-MAIN"
        });
        
        var inventoryResult = await checkInventoryTool.ExecuteAsync(inventoryInput, context, CancellationToken.None);
        
        inventoryResult.Success.Should().BeTrue();
        var inventory = inventoryResult.Data as CheckInventoryOutput;
        inventory.Should().NotBeNull();
        inventory!.Available.Should().BeGreaterThan(50);
        
        // Step 4: Simulate Price
        var simulatePriceTool = _fixture.Services.GetRequiredService<SimulatePriceTool>();
        var priceInput = JsonSerializer.SerializeToElement(new 
        { 
            customerAccount = "CUST-001",
            itemId = "ITEM-100",
            quantity = 50
        });
        
        var priceResult = await simulatePriceTool.ExecuteAsync(priceInput, context, CancellationToken.None);
        
        priceResult.Success.Should().BeTrue();
        var price = priceResult.Data as SimulatePriceOutput;
        price.Should().NotBeNull();
        price!.FinalUnitPrice.Should().BeLessThan(200); // Should have discount
        price.TotalDiscountPct.Should().BeGreaterThan(0);
        
        // Step 5: Create Order
        var createOrderTool = _fixture.Services.GetRequiredService<CreateSalesOrderTool>();
        var orderInput = JsonSerializer.SerializeToElement(new 
        { 
            customerAccount = "CUST-001",
            lines = new[]
            {
                new { itemId = "ITEM-100", quantity = 50 }
            },
            idempotencyKey = Guid.NewGuid().ToString()
        });
        
        var orderResult = await createOrderTool.ExecuteAsync(orderInput, context, CancellationToken.None);
        
        orderResult.Success.Should().BeTrue();
        var order = orderResult.Data as CreateSalesOrderOutput;
        order.Should().NotBeNull();
        order!.SalesId.Should().StartWith("SO-");
        order.LinesCreated.Should().Be(1);
        order.TotalAmount.Should().BeGreaterThan(0);
        
        // Verify order was created in mock
        _fixture.WcfClient.CreatedOrders.Should().Contain(order.SalesId);
    }
    
    [Fact]
    public async Task OrderCreation_WithBlockedCustomer_ShouldFail()
    {
        var context = new ToolContext
        {
            UserId = "test-user",
            Roles = new[] { "MCP_Write" },
            CorrelationId = Guid.NewGuid().ToString()
        };
        
        var createOrderTool = _fixture.Services.GetRequiredService<CreateSalesOrderTool>();
        var orderInput = JsonSerializer.SerializeToElement(new 
        { 
            customerAccount = "CUST-BLOCKED",
            lines = new[] { new { itemId = "ITEM-100", quantity = 10 } },
            idempotencyKey = Guid.NewGuid().ToString()
        });
        
        var result = await createOrderTool.ExecuteAsync(orderInput, context, CancellationToken.None);
        
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("CUST_BLOCKED");
    }
    
    [Fact]
    public async Task OrderCreation_WithBlockedItem_ShouldFail()
    {
        var context = new ToolContext
        {
            UserId = "test-user",
            Roles = new[] { "MCP_Write" },
            CorrelationId = Guid.NewGuid().ToString()
        };
        
        var createOrderTool = _fixture.Services.GetRequiredService<CreateSalesOrderTool>();
        var orderInput = JsonSerializer.SerializeToElement(new 
        { 
            customerAccount = "CUST-001",
            lines = new[] { new { itemId = "ITEM-BLOCKED", quantity = 10 } },
            idempotencyKey = Guid.NewGuid().ToString()
        });
        
        var result = await createOrderTool.ExecuteAsync(orderInput, context, CancellationToken.None);
        
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("ITEM_BLOCKED");
    }
    
    [Fact]
    public async Task CreditCheck_ExceedingLimit_ShouldWarn()
    {
        var context = new ToolContext
        {
            UserId = "test-user",
            Roles = new[] { "MCP_Read" },
            CorrelationId = Guid.NewGuid().ToString()
        };
        
        var checkCreditTool = _fixture.Services.GetRequiredService<CheckCreditTool>();
        var creditInput = JsonSerializer.SerializeToElement(new 
        { 
            customerAccount = "CUST-002", // Near limit
            proposedAmount = 10000
        });
        
        var result = await checkCreditTool.ExecuteAsync(creditInput, context, CancellationToken.None);
        
        result.Success.Should().BeTrue();
        var credit = result.Data as CheckCreditOutput;
        credit.Should().NotBeNull();
        credit!.WouldExceedLimit.Should().BeTrue();
        credit.Recommendation.Should().Contain("REJECT");
    }
    
    [Fact]
    public async Task IdempotentOrderCreation_ShouldReturnSameResult()
    {
        var context = new ToolContext
        {
            UserId = "test-user",
            Roles = new[] { "MCP_Write" },
            CorrelationId = Guid.NewGuid().ToString()
        };
        
        var idempotencyKey = Guid.NewGuid().ToString();
        var createOrderTool = _fixture.Services.GetRequiredService<CreateSalesOrderTool>();
        var orderInput = JsonSerializer.SerializeToElement(new 
        { 
            customerAccount = "CUST-001",
            lines = new[] { new { itemId = "ITEM-100", quantity = 10 } },
            idempotencyKey
        });
        
        // First call
        var result1 = await createOrderTool.ExecuteAsync(orderInput, context, CancellationToken.None);
        result1.Success.Should().BeTrue();
        var order1 = result1.Data as CreateSalesOrderOutput;
        
        // Second call with same key
        var result2 = await createOrderTool.ExecuteAsync(orderInput, context, CancellationToken.None);
        result2.Success.Should().BeTrue();
        var order2 = result2.Data as CreateSalesOrderOutput;
        
        // Should return same order
        order2!.SalesId.Should().Be(order1!.SalesId);
        order2.Duplicate.Should().BeTrue();
    }
}
