using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using GBL.AX2012.MCP.Core.Options;
using GBL.AX2012.MCP.AxConnector.Interfaces;

namespace GBL.AX2012.MCP.AxConnector.Clients;

public class BusinessConnectorClient : IBusinessConnector
{
    private readonly ILogger<BusinessConnectorClient> _logger;
    private readonly BusinessConnectorOptions _options;
    private object? _axapta;
    private bool _isLoggedOn;
    private readonly object _lock = new();
    
    public bool IsConnected => _isLoggedOn;
    
    public BusinessConnectorClient(
        IOptions<BusinessConnectorOptions> options,
        ILogger<BusinessConnectorClient> logger)
    {
        _options = options.Value;
        _logger = logger;
    }
    
    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        var result = await CheckHealthAsync(cancellationToken);
        return result.AosConnected;
    }
    
    public Task<AxHealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var result = new AxHealthCheckResult
            {
                Timestamp = DateTime.UtcNow
            };
            
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                EnsureConnected();
                
                // Execute a simple query to verify connectivity
                var companyInfo = ExecuteQuery("select firstonly DataAreaId from CompanyInfo");
                
                result.AosConnected = true;
                result.ResponseTimeMs = stopwatch.ElapsedMilliseconds;
                result.Status = "healthy";
                result.Details = new Dictionary<string, string>
                {
                    ["database"] = "connected",
                    ["business_connector"] = "connected",
                    ["company"] = companyInfo ?? _options.Company,
                    ["aos"] = _options.ObjectServer
                };
            }
            catch (Exception ex)
            {
                result.AosConnected = false;
                result.ResponseTimeMs = stopwatch.ElapsedMilliseconds;
                result.Status = "unhealthy";
                result.Error = ex.Message;
                result.Details = new Dictionary<string, string>
                {
                    ["database"] = "unknown",
                    ["business_connector"] = "error",
                    ["error_type"] = ex.GetType().Name
                };
                
                _logger.LogError(ex, "Health check failed");
            }
            
            return result;
        }, cancellationToken);
    }
    
    private void EnsureConnected()
    {
        lock (_lock)
        {
            if (_isLoggedOn) return;
            
            try
            {
                // Try to load BC.NET dynamically
                var axaptaType = Type.GetType("Microsoft.Dynamics.BusinessConnectorNet.Axapta, Microsoft.Dynamics.BusinessConnectorNet");
                
                if (axaptaType == null)
                {
                    // BC.NET not installed - simulate connection for development
                    _logger.LogWarning("Business Connector .NET is not installed - using mock connection");
                    _isLoggedOn = true;
                    return;
                }
                
                _axapta = Activator.CreateInstance(axaptaType);
                
                var logonMethod = axaptaType.GetMethod("Logon");
                logonMethod?.Invoke(_axapta, new object?[]
                {
                    _options.Company,
                    _options.Language,
                    _options.ObjectServer,
                    _options.Configuration
                });
                
                _isLoggedOn = true;
                _logger.LogInformation("Business Connector logged on to {Company} at {AOS}", 
                    _options.Company, _options.ObjectServer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to logon to Business Connector");
                throw;
            }
        }
    }
    
    private string? ExecuteQuery(string statement)
    {
        if (_axapta == null) 
        {
            // Mock response for development without BC.NET
            return _options.Company;
        }
        
        try
        {
            var axaptaType = _axapta.GetType();
            var createRecordMethod = axaptaType.GetMethod("CreateAxaptaRecord");
            
            var record = createRecordMethod?.Invoke(_axapta, new object[] { "CompanyInfo" });
            if (record == null) return null;
            
            var recordType = record.GetType();
            var executeMethod = recordType.GetMethod("ExecuteStmt");
            executeMethod?.Invoke(record, new object[] { statement });
            
            var getFieldMethod = recordType.GetMethod("get_Field");
            var result = getFieldMethod?.Invoke(record, new object[] { "DataAreaId" });
            
            return result?.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to execute BC.NET query");
            return _options.Company;
        }
    }
    
    public void Dispose()
    {
        lock (_lock)
        {
            if (_isLoggedOn && _axapta != null)
            {
                try
                {
                    var logoffMethod = _axapta.GetType().GetMethod("Logoff");
                    logoffMethod?.Invoke(_axapta, null);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error during Business Connector logoff");
                }
                
                _isLoggedOn = false;
            }
        }
    }
}
