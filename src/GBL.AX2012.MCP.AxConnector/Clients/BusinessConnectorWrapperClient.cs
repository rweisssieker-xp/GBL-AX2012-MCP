using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using GBL.AX2012.MCP.Core.Options;
using GBL.AX2012.MCP.AxConnector.Interfaces;

namespace GBL.AX2012.MCP.AxConnector.Clients;

/// <summary>
/// HTTP Client that communicates with the .NET Framework BC.Wrapper service
/// </summary>
public class BusinessConnectorWrapperClient : IBusinessConnector
{
    private readonly HttpClient _httpClient;
    private readonly BusinessConnectorOptions _options;
    private readonly ILogger<BusinessConnectorWrapperClient> _logger;
    private readonly string _wrapperBaseUrl;
    
    public bool IsConnected { get; private set; }
    
    public BusinessConnectorWrapperClient(
        HttpClient httpClient,
        IOptions<BusinessConnectorOptions> options,
        ILogger<BusinessConnectorWrapperClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        
        // Get wrapper URL from configuration or environment
        _wrapperBaseUrl = options.Value.WrapperUrl 
            ?? Environment.GetEnvironmentVariable("BC_WRAPPER_URL") 
            ?? "http://localhost:8090";
        
        _httpClient.BaseAddress = new Uri(_wrapperBaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }
    
    public async Task<AxHealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new
            {
                Company = _options.Company,
                ObjectServer = _options.ObjectServer,
                Language = _options.Language,
                Configuration = _options.Configuration
            };
            
            var response = await _httpClient.PostAsJsonAsync(
                "/api/health/check", 
                request, 
                cancellationToken);
            
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadFromJsonAsync<AxHealthCheckResult>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                cancellationToken);
            
            if (result != null)
            {
                IsConnected = result.AosConnected;
                return result;
            }
            
            throw new InvalidOperationException("Invalid response from BC.Wrapper service");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to communicate with BC.Wrapper service at {Url}", _wrapperBaseUrl);
            
            return new AxHealthCheckResult
            {
                Status = "unhealthy",
                AosConnected = false,
                ResponseTimeMs = 0,
                Timestamp = DateTime.UtcNow,
                Error = $"BC.Wrapper service unavailable: {ex.Message}",
                Details = new Dictionary<string, string>
                {
                    ["wrapper_url"] = _wrapperBaseUrl,
                    ["error_type"] = "ServiceUnavailable"
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in health check");
            
            return new AxHealthCheckResult
            {
                Status = "unhealthy",
                AosConnected = false,
                ResponseTimeMs = 0,
                Timestamp = DateTime.UtcNow,
                Error = ex.Message,
                Details = new Dictionary<string, string>
                {
                    ["error_type"] = ex.GetType().Name
                }
            };
        }
    }
    
    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        var result = await CheckHealthAsync(cancellationToken);
        return result.AosConnected;
    }
    
    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

