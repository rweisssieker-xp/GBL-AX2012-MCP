using System.Web.Http;
using GBL.AX2012.MCP.BC.Wrapper.Models;
using GBL.AX2012.MCP.BC.Wrapper.Services;

namespace GBL.AX2012.MCP.BC.Wrapper.Controllers;

[RoutePrefix("api/health")]
public class HealthCheckController : ApiController
{
    private readonly BusinessConnectorService _bcService;
    private readonly ILogger<HealthCheckController> _logger;
    
    public HealthCheckController(
        BusinessConnectorService bcService,
        ILogger<HealthCheckController> logger)
    {
        _bcService = bcService;
        _logger = logger;
    }
    
    [HttpPost]
    [Route("check")]
    public IHttpActionResult CheckHealth([FromBody] HealthCheckRequest request)
    {
        try
        {
            var response = _bcService.CheckHealth(request ?? new HealthCheckRequest());
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in health check endpoint");
            return InternalServerError(ex);
        }
    }
    
    [HttpGet]
    [Route("status")]
    public IHttpActionResult GetStatus()
    {
        return Ok(new { 
            service = "BC.Wrapper", 
            status = "running",
            connected = _bcService.IsConnected,
            timestamp = DateTime.UtcNow
        });
    }
}

