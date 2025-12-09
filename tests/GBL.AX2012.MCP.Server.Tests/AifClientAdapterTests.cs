using System.Net.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using FluentAssertions;
using GBL.AX2012.MCP.Core.Exceptions;
using GBL.AX2012.MCP.Core.Interfaces;
using GBL.AX2012.MCP.Core.Options;
using GBL.AX2012.MCP.AxConnector.Clients;
using GBL.AX2012.MCP.AxConnector.Interfaces;
using GBL.AX2012.MCP.Core.Models;

namespace GBL.AX2012.MCP.Server.Tests;

public class AifClientAdapterTests
{
    private readonly Mock<AifClient> _httpClient;
    private readonly Mock<AifNetTcpClient> _netTcpClient;
    private readonly Mock<ILogger<AifClientAdapter>> _logger;
    private readonly AifClientOptions _options;
    
    public AifClientAdapterTests()
    {
        _httpClient = new Mock<AifClient>(
            Mock.Of<HttpClient>(),
            Options.Create(new AifClientOptions()),
            Mock.Of<ILogger<AifClient>>(),
            Mock.Of<ICircuitBreaker>());
        
        _netTcpClient = new Mock<AifNetTcpClient>(
            Options.Create(new AifClientOptions()),
            Mock.Of<ILogger<AifNetTcpClient>>(),
            Mock.Of<ICircuitBreaker>());
        
        _logger = new Mock<ILogger<AifClientAdapter>>();
        _options = new AifClientOptions
        {
            FallbackStrategy = "auto"
        };
    }
    
    [Fact]
    public async Task GetCustomerAsync_HttpSucceeds_UsesHttp()
    {
        // Arrange
        var expectedCustomer = new Customer
        {
            AccountNum = "CUST-001",
            Name = "Test Customer"
        };
        
        _httpClient.Setup(x => x.GetCustomerAsync("CUST-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCustomer);
        
        // Create real adapter with mocked clients
        var httpClientInstance = _httpClient.Object;
        var netTcpClientInstance = _netTcpClient.Object;
        
        // We need to create adapter with real AifClient and AifNetTcpClient instances
        // For testing, we'll use the interface mocks directly
        var adapter = new AifClientAdapter(
            httpClientInstance as AifClient ?? throw new InvalidOperationException("Mock setup issue"),
            netTcpClientInstance as AifNetTcpClient ?? throw new InvalidOperationException("Mock setup issue"),
            Options.Create(_options),
            _logger.Object);
        
        // Act
        var result = await adapter.GetCustomerAsync("CUST-001");
        
        // Assert
        result.Should().NotBeNull();
        result!.AccountNum.Should().Be("CUST-001");
        _httpClient.Verify(x => x.GetCustomerAsync("CUST-001", It.IsAny<CancellationToken>()), Times.Once);
        _netTcpClient.Verify(x => x.GetCustomerAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
    
    [Fact]
    public async Task GetCustomerAsync_HttpFails_FallsBackToNetTcp()
    {
        // Arrange
        var expectedCustomer = new Customer
        {
            AccountNum = "CUST-001",
            Name = "Test Customer"
        };
        
        _httpClient.Setup(x => x.GetCustomerAsync("CUST-001", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AxException("HTTP_ERROR", "HTTP failed"));
        
        _netTcpClient.Setup(x => x.GetCustomerAsync("CUST-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCustomer);
        
        var adapter = new AifClientAdapter(
            _httpClient.Object,
            _netTcpClient.Object,
            Options.Create(_options),
            _logger.Object);
        
        // Act
        var result = await adapter.GetCustomerAsync("CUST-001");
        
        // Assert
        result.Should().NotBeNull();
        result!.AccountNum.Should().Be("CUST-001");
        _httpClient.Verify(x => x.GetCustomerAsync("CUST-001", It.IsAny<CancellationToken>()), Times.Once);
        _netTcpClient.Verify(x => x.GetCustomerAsync("CUST-001", It.IsAny<CancellationToken>()), Times.Once);
    }
    
    [Fact]
    public async Task GetCustomerAsync_UseNetTcp_UsesNetTcpDirectly()
    {
        // Arrange
        var expectedCustomer = new Customer
        {
            AccountNum = "CUST-001",
            Name = "Test Customer"
        };
        
        _options.UseNetTcp = true;
        
        _netTcpClient.Setup(x => x.GetCustomerAsync("CUST-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCustomer);
        
        var adapter = new AifClientAdapter(
            _httpClient.Object,
            _netTcpClient.Object,
            Options.Create(_options),
            _logger.Object);
        
        // Act
        var result = await adapter.GetCustomerAsync("CUST-001");
        
        // Assert
        result.Should().NotBeNull();
        result!.AccountNum.Should().Be("CUST-001");
        _netTcpClient.Verify(x => x.GetCustomerAsync("CUST-001", It.IsAny<CancellationToken>()), Times.Once);
    }
}

