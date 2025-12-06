using GBL.AX2012.MCP.AxConnector.Interfaces;

namespace GBL.AX2012.MCP.Integration.Tests.Mocks;

public class MockBusinessConnector : IBusinessConnector
{
    public bool IsConnected => true;
    
    public Task<AxHealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new AxHealthCheckResult
        {
            Status = "healthy",
            AosConnected = true,
            ResponseTimeMs = 25,
            Details = new Dictionary<string, string>
            {
                ["company"] = "DAT",
                ["aos"] = "mock-aos:2712",
                ["version"] = "6.3.6000.7364"
            }
        });
    }
    
    public void Dispose()
    {
    }
}
