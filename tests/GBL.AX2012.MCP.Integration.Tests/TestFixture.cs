using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using GBL.AX2012.MCP.Core.Interfaces;
using GBL.AX2012.MCP.Core.Options;
using GBL.AX2012.MCP.Server.Middleware;
using GBL.AX2012.MCP.Server.Security;
using GBL.AX2012.MCP.Server.Tools;
using GBL.AX2012.MCP.Server.Approval;
using GBL.AX2012.MCP.AxConnector.Interfaces;
using GBL.AX2012.MCP.Audit.Services;
using GBL.AX2012.MCP.Integration.Tests.Mocks;

namespace GBL.AX2012.MCP.Integration.Tests;

public class TestFixture : IDisposable
{
    public IServiceProvider Services { get; }
    public MockAifClient AifClient { get; }
    public MockWcfClient WcfClient { get; }
    public MockBusinessConnector BusinessConnector { get; }
    
    public TestFixture()
    {
        AifClient = new MockAifClient();
        WcfClient = new MockWcfClient();
        BusinessConnector = new MockBusinessConnector();
        
        var services = new ServiceCollection();
        
        // Logging
        services.AddLogging(builder => builder.AddDebug());
        
        // Options
        services.Configure<McpServerOptions>(o => { o.ServerName = "test"; o.ServerVersion = "1.0.0"; });
        services.Configure<RateLimiterOptions>(o => { o.RequestsPerMinute = 1000; o.Enabled = true; });
        services.Configure<CircuitBreakerOptions>(o => { o.FailureThreshold = 5; o.OpenDuration = TimeSpan.FromSeconds(30); });
        services.Configure<AuditOptions>(o => { o.FileLogPath = "logs/test-audit"; });
        services.Configure<SecurityOptions>(o => { o.ApprovalThreshold = 50000; });
        
        // Middleware
        services.AddSingleton<IRateLimiter, RateLimiter>();
        services.AddSingleton<ICircuitBreaker, CircuitBreaker>();
        services.AddSingleton<IIdempotencyStore, MemoryIdempotencyStore>();
        
        // Security
        services.AddSingleton<IAuthorizationService, AuthorizationService>();
        services.AddSingleton<IApprovalService, ApprovalService>();
        
        // Audit
        services.AddSingleton<IAuditService, FileAuditService>();
        
        // Mock AX Connectors
        services.AddSingleton<IAifClient>(AifClient);
        services.AddSingleton<IWcfClient>(WcfClient);
        services.AddSingleton<IBusinessConnector>(BusinessConnector);
        
        // Validators
        services.AddSingleton<GetCustomerInputValidator>();
        services.AddSingleton<GetSalesOrderInputValidator>();
        services.AddSingleton<CheckInventoryInputValidator>();
        services.AddSingleton<SimulatePriceInputValidator>();
        services.AddSingleton<CreateSalesOrderInputValidator>();
        services.AddSingleton<CheckCreditInputValidator>();
        services.AddSingleton<GetItemInputValidator>();
        services.AddSingleton<UpdateSalesOrderInputValidator>();
        
        // Tools
        services.AddSingleton<HealthCheckTool>();
        services.AddSingleton<GetCustomerTool>();
        services.AddSingleton<GetSalesOrderTool>();
        services.AddSingleton<CheckInventoryTool>();
        services.AddSingleton<SimulatePriceTool>();
        services.AddSingleton<CreateSalesOrderTool>();
        services.AddSingleton<CheckCreditTool>();
        services.AddSingleton<GetItemTool>();
        services.AddSingleton<UpdateSalesOrderTool>();
        
        Services = services.BuildServiceProvider();
    }
    
    public void Dispose()
    {
        if (Services is IDisposable disposable)
            disposable.Dispose();
    }
}
