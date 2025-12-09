using System.Diagnostics;
using GBL.AX2012.MCP.BC.Wrapper.Models;

namespace GBL.AX2012.MCP.BC.Wrapper.Services;

// Note: Microsoft.Dynamics.BusinessConnectorNet is referenced via project file
// This allows the wrapper to use BC.NET while running on .NET Framework

public class BusinessConnectorService : IDisposable
{
    private object? _axapta; // Using object with reflection to avoid compile-time dependency
    private bool _isLoggedOn;
    private readonly object _lock = new();
    private readonly ILogger<BusinessConnectorService> _logger;
    
    public bool IsConnected => _isLoggedOn;
    
    public BusinessConnectorService(ILogger<BusinessConnectorService> logger)
    {
        _logger = logger;
    }
    
    public HealthCheckResponse CheckHealth(HealthCheckRequest request)
    {
        var result = new HealthCheckResponse
        {
            Timestamp = DateTime.UtcNow
        };
        
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            EnsureConnected(request);
            
            // Execute a simple query to verify connectivity
            var companyInfo = ExecuteQuery("select firstonly DataAreaId from CompanyInfo");
            
            result.AosConnected = true;
            result.ResponseTimeMs = stopwatch.ElapsedMilliseconds;
            result.Status = "healthy";
            result.Details = new Dictionary<string, string>
            {
                ["database"] = "connected",
                ["business_connector"] = "connected",
                ["company"] = companyInfo ?? request.Company ?? "unknown",
                ["aos"] = request.ObjectServer ?? "unknown"
            };
            
            _logger.LogInformation("Health check successful: {Company} at {AOS}", 
                companyInfo ?? request.Company, request.ObjectServer);
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
    }
    
    private void EnsureConnected(HealthCheckRequest request)
    {
        lock (_lock)
        {
            if (_isLoggedOn && _axapta != null) return;
            
            try
            {
                // Load BC.NET type dynamically
                var axaptaType = Type.GetType("Microsoft.Dynamics.BusinessConnectorNet.Axapta, Microsoft.Dynamics.BusinessConnectorNet");
                if (axaptaType == null)
                {
                    throw new InvalidOperationException("Business Connector .NET is not installed or not found in GAC");
                }
                
                _axapta = Activator.CreateInstance(axaptaType);
                
                var logonMethod = axaptaType.GetMethod("Logon");
                logonMethod?.Invoke(_axapta, new object?[]
                {
                    request.Company ?? "DAT",
                    request.Language ?? "en-us",
                    request.ObjectServer ?? "ax-aos:2712",
                    request.Configuration
                });
                
                _isLoggedOn = true;
                _logger.LogInformation("Business Connector logged on to {Company} at {AOS}", 
                    request.Company ?? "DAT", request.ObjectServer ?? "ax-aos:2712");
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
        if (_axapta == null) return null;
        
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
            return null;
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
                
                _axapta = null;
                _isLoggedOn = false;
            }
        }
    }
}

