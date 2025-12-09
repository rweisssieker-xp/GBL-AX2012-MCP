using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using GBL.AX2012.MCP.Core.Options;
using GBL.AX2012.MCP.Audit.Data;

namespace GBL.AX2012.MCP.Server.Configuration;

public class ConfigurationValidator
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConfigurationValidator> _logger;
    
    public ConfigurationValidator(
        IConfiguration configuration,
        ILogger<ConfigurationValidator> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }
    
    public async Task ValidateAsync()
    {
        _logger.LogInformation("Starting configuration validation...");
        
        var errors = new List<string>();
        
        // Validate Database Connection
        try
        {
            await ValidateDatabaseConnectionAsync();
            _logger.LogInformation("✓ Database connection validated");
        }
        catch (Exception ex)
        {
            var error = $"Database connection failed: {ex.Message}";
            errors.Add(error);
            _logger.LogWarning(ex, "✗ {Error}", error);
        }
        
        // Validate AIF Client Configuration
        try
        {
            ValidateAifClientConfiguration();
            _logger.LogInformation("✓ AIF Client configuration validated");
        }
        catch (Exception ex)
        {
            var error = $"AIF Client configuration invalid: {ex.Message}";
            errors.Add(error);
            _logger.LogWarning(ex, "✗ {Error}", error);
        }
        
        // Validate WCF Client Configuration
        try
        {
            ValidateWcfClientConfiguration();
            _logger.LogInformation("✓ WCF Client configuration validated");
        }
        catch (Exception ex)
        {
            var error = $"WCF Client configuration invalid: {ex.Message}";
            errors.Add(error);
            _logger.LogWarning(ex, "✗ {Error}", error);
        }
        
        // Validate Business Connector Configuration
        try
        {
            ValidateBusinessConnectorConfiguration();
            _logger.LogInformation("✓ Business Connector configuration validated");
        }
        catch (Exception ex)
        {
            var error = $"Business Connector configuration invalid: {ex.Message}";
            errors.Add(error);
            _logger.LogWarning(ex, "✗ {Error}", error);
        }
        
        // Validate Webhook Configuration
        try
        {
            ValidateWebhookConfiguration();
            _logger.LogInformation("✓ Webhook configuration validated");
        }
        catch (Exception ex)
        {
            var error = $"Webhook configuration invalid: {ex.Message}";
            errors.Add(error);
            _logger.LogWarning(ex, "✗ {Error}", error);
        }
        
        // Validate URLs
        try
        {
            ValidateUrls();
            _logger.LogInformation("✓ URLs validated");
        }
        catch (Exception ex)
        {
            var error = $"URL validation failed: {ex.Message}";
            errors.Add(error);
            _logger.LogWarning(ex, "✗ {Error}", error);
        }
        
        if (errors.Count > 0)
        {
            var errorMessage = string.Join("\n", errors);
            _logger.LogError("Configuration validation failed with {Count} error(s):\n{Errors}", 
                errors.Count, errorMessage);
            throw new InvalidOperationException($"Configuration validation failed:\n{errorMessage}");
        }
        
        _logger.LogInformation("✓ Configuration validation completed successfully");
    }
    
    private async Task ValidateDatabaseConnectionAsync()
    {
        var connectionString = _configuration.GetConnectionString("AuditDb");
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("AuditDb connection string is not configured");
        }
        
        try
        {
            var optionsBuilder = new DbContextOptionsBuilder<WebhookDbContext>();
            optionsBuilder.UseSqlServer(connectionString);
            
            using var context = new WebhookDbContext(optionsBuilder.Options);
            await context.Database.CanConnectAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Cannot connect to database: {ex.Message}", ex);
        }
    }
    
    private void ValidateAifClientConfiguration()
    {
        var options = _configuration.GetSection(AifClientOptions.SectionName).Get<AifClientOptions>();
        if (options == null)
        {
            throw new InvalidOperationException("AifClient configuration section is missing");
        }
        
        if (string.IsNullOrEmpty(options.BaseUrl))
        {
            throw new InvalidOperationException("AifClient.BaseUrl is not configured");
        }
        
        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException($"AifClient.BaseUrl is not a valid URL: {options.BaseUrl}");
        }
        
        if (options.UseNetTcp && options.NetTcpPort.HasValue)
        {
            if (options.NetTcpPort < 1 || options.NetTcpPort > 65535)
            {
                throw new InvalidOperationException($"AifClient.NetTcpPort must be between 1 and 65535: {options.NetTcpPort}");
            }
        }
    }
    
    private void ValidateWcfClientConfiguration()
    {
        var options = _configuration.GetSection(WcfClientOptions.SectionName).Get<WcfClientOptions>();
        if (options == null)
        {
            throw new InvalidOperationException("WcfClient configuration section is missing");
        }
        
        if (string.IsNullOrEmpty(options.BaseUrl))
        {
            throw new InvalidOperationException("WcfClient.BaseUrl is not configured");
        }
        
        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException($"WcfClient.BaseUrl is not a valid URL: {options.BaseUrl}");
        }
    }
    
    private void ValidateBusinessConnectorConfiguration()
    {
        var options = _configuration.GetSection(BusinessConnectorOptions.SectionName).Get<BusinessConnectorOptions>();
        if (options == null)
        {
            throw new InvalidOperationException("BusinessConnector configuration section is missing");
        }
        
        if (options.UseWrapper)
        {
            if (string.IsNullOrEmpty(options.WrapperUrl))
            {
                throw new InvalidOperationException("BusinessConnector.WrapperUrl is required when UseWrapper is true");
            }
            
            if (!Uri.TryCreate(options.WrapperUrl, UriKind.Absolute, out var uri))
            {
                throw new InvalidOperationException($"BusinessConnector.WrapperUrl is not a valid URL: {options.WrapperUrl}");
            }
            
            if (uri.Scheme != "http" && uri.Scheme != "https")
            {
                throw new InvalidOperationException($"BusinessConnector.WrapperUrl must use http or https scheme: {options.WrapperUrl}");
            }
        }
    }
    
    private void ValidateWebhookConfiguration()
    {
        var webhookSection = _configuration.GetSection("Webhooks");
        if (webhookSection.Exists())
        {
            var maxConcurrent = webhookSection.GetValue<int?>("MaxConcurrentDeliveries");
            if (maxConcurrent.HasValue && maxConcurrent < 1)
            {
                throw new InvalidOperationException("Webhooks.MaxConcurrentDeliveries must be greater than 0");
            }
            
            var timeout = webhookSection.GetValue<int?>("DeliveryTimeoutSeconds");
            if (timeout.HasValue && timeout < 1)
            {
                throw new InvalidOperationException("Webhooks.DeliveryTimeoutSeconds must be greater than 0");
            }
        }
    }
    
    private void ValidateUrls()
    {
        // Validate all configured URLs are valid
        var urlsToValidate = new[]
        {
            ("AifClient.BaseUrl", _configuration["AifClient:BaseUrl"]),
            ("WcfClient.BaseUrl", _configuration["WcfClient:BaseUrl"]),
            ("BusinessConnector.WrapperUrl", _configuration["BusinessConnector:WrapperUrl"]),
            ("HttpTransport", _configuration["HttpTransport:Port"] != null 
                ? $"http://localhost:{_configuration["HttpTransport:Port"]}" 
                : null)
        };
        
        foreach (var (name, url) in urlsToValidate)
        {
            if (!string.IsNullOrEmpty(url))
            {
                if (!Uri.TryCreate(url, UriKind.Absolute, out _))
                {
                    throw new InvalidOperationException($"{name} is not a valid URL: {url}");
                }
            }
        }
    }
}

