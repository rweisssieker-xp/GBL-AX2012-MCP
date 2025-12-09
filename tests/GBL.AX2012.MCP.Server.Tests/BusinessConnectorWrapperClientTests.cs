using System.Net;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Xunit;
using FluentAssertions;
using GBL.AX2012.MCP.Core.Options;
using GBL.AX2012.MCP.AxConnector.Clients;
using GBL.AX2012.MCP.AxConnector.Interfaces;

namespace GBL.AX2012.MCP.Server.Tests;

public class BusinessConnectorWrapperClientTests
{
    private readonly Mock<ILogger<BusinessConnectorWrapperClient>> _logger;
    private readonly BusinessConnectorOptions _options;
    
    public BusinessConnectorWrapperClientTests()
    {
        _logger = new Mock<ILogger<BusinessConnectorWrapperClient>>();
        _options = new BusinessConnectorOptions
        {
            Company = "DAT",
            ObjectServer = "ax-aos:2712",
            Language = "en-us",
            WrapperUrl = "http://localhost:8090"
        };
    }
    
    [Fact]
    public async Task CheckHealthAsync_ValidResponse_ReturnsHealthCheckResult()
    {
        // Arrange
        var expectedResult = new AxHealthCheckResult
        {
            Status = "healthy",
            AosConnected = true,
            ResponseTimeMs = 123,
            Timestamp = DateTime.UtcNow,
            Details = new Dictionary<string, string>
            {
                ["company"] = "DAT",
                ["aos"] = "ax-aos:2712"
            }
        };
        
        var httpMessageHandler = new Mock<HttpMessageHandler>();
        httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(expectedResult))
            });
        
        var httpClient = new HttpClient(httpMessageHandler.Object)
        {
            BaseAddress = new Uri("http://localhost:8090")
        };
        
        var client = new BusinessConnectorWrapperClient(
            httpClient,
            Options.Create(_options),
            _logger.Object);
        
        // Act
        var result = await client.CheckHealthAsync();
        
        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be("healthy");
        result.AosConnected.Should().BeTrue();
        client.IsConnected.Should().BeTrue();
    }
    
    [Fact]
    public async Task CheckHealthAsync_ServiceUnavailable_ReturnsUnhealthy()
    {
        // Arrange
        var httpMessageHandler = new Mock<HttpMessageHandler>();
        httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Service unavailable"));
        
        var httpClient = new HttpClient(httpMessageHandler.Object)
        {
            BaseAddress = new Uri("http://localhost:8090")
        };
        
        var client = new BusinessConnectorWrapperClient(
            httpClient,
            Options.Create(_options),
            _logger.Object);
        
        // Act
        var result = await client.CheckHealthAsync();
        
        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be("unhealthy");
        result.AosConnected.Should().BeFalse();
        result.Error.Should().Contain("ServiceUnavailable");
        client.IsConnected.Should().BeFalse();
    }
    
    [Fact]
    public async Task TestConnectionAsync_Healthy_ReturnsTrue()
    {
        // Arrange
        var expectedResult = new AxHealthCheckResult
        {
            Status = "healthy",
            AosConnected = true
        };
        
        var httpMessageHandler = new Mock<HttpMessageHandler>();
        httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(expectedResult))
            });
        
        var httpClient = new HttpClient(httpMessageHandler.Object)
        {
            BaseAddress = new Uri("http://localhost:8090")
        };
        
        var client = new BusinessConnectorWrapperClient(
            httpClient,
            Options.Create(_options),
            _logger.Object);
        
        // Act
        var result = await client.TestConnectionAsync();
        
        // Assert
        result.Should().BeTrue();
    }
}

