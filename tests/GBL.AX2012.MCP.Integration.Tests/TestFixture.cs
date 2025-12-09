using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using GBL.AX2012.MCP.Core.Interfaces;
using GBL.AX2012.MCP.Core.Options;
using GBL.AX2012.MCP.Server.Middleware;
using GBL.AX2012.MCP.Server.Security;
using GBL.AX2012.MCP.Server.Tools;
using GBL.AX2012.MCP.Server.Approval;
using GBL.AX2012.MCP.Server.Events;
using GBL.AX2012.MCP.Server.Webhooks;
using GBL.AX2012.MCP.Server.Resilience;
using GBL.AX2012.MCP.AxConnector.Interfaces;
using GBL.AX2012.MCP.Audit.Services;
using GBL.AX2012.MCP.Audit.Data;
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
        services.Configure<WebhookServiceOptions>(o => { o.MaxConcurrentDeliveries = 10; o.DeliveryTimeoutSeconds = 30; });
        
        // Middleware
        services.AddSingleton<IRateLimiter, RateLimiter>();
        services.AddSingleton<ICircuitBreaker, CircuitBreaker>();
        services.AddSingleton<IIdempotencyStore, MemoryIdempotencyStore>();
        
        // Security
        services.AddSingleton<IAuthorizationService, AuthorizationService>();
        services.AddSingleton<IApprovalService, ApprovalService>();
        
        // Audit
        services.AddSingleton<IAuditService, FileAuditService>();
        
        // Event Bus
        services.AddSingleton<IEventBus, EventBus>();
        
        // Webhook Service (InMemory DB for tests)
        services.AddDbContextFactory<WebhookDbContext>(options =>
            options.UseInMemoryDatabase("TestWebhooks"));
        services.AddHttpClient();
        services.AddSingleton<IWebhookService, DatabaseWebhookService>();
        
        // Self-Healing Services
        services.AddSingleton<IConnectionPoolMonitor, ConnectionPoolMonitor>();
        services.AddSingleton<ISelfHealingService, SelfHealingService>();
        
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
        services.AddSingleton<BatchOperationsInputValidator>();
        services.AddSingleton<SubscribeWebhookInputValidator>();
        services.AddSingleton<UnsubscribeWebhookInputValidator>();
        services.AddSingleton<GetRoiMetricsInputValidator>();
        services.AddSingleton<BulkImportInputValidator>();
        
        // Tools - Existing
        services.AddSingleton<HealthCheckTool>();
        services.AddSingleton<GetCustomerTool>();
        services.AddSingleton<GetSalesOrderTool>();
        services.AddSingleton<CheckInventoryTool>();
        services.AddSingleton<SimulatePriceTool>();
        services.AddSingleton<CreateSalesOrderTool>();
        services.AddSingleton<CheckCreditTool>();
        services.AddSingleton<GetItemTool>();
        services.AddSingleton<UpdateSalesOrderTool>();
        
        // Tools - Epic 7: Batch Operations & Webhooks
        var serviceProvider = services.BuildServiceProvider();
        var allTools = new List<ITool>
        {
            serviceProvider.GetRequiredService<HealthCheckTool>(),
            serviceProvider.GetRequiredService<GetCustomerTool>(),
            serviceProvider.GetRequiredService<GetSalesOrderTool>(),
            serviceProvider.GetRequiredService<CheckInventoryTool>(),
            serviceProvider.GetRequiredService<SimulatePriceTool>(),
            serviceProvider.GetRequiredService<CreateSalesOrderTool>(),
            serviceProvider.GetRequiredService<CheckCreditTool>(),
            serviceProvider.GetRequiredService<GetItemTool>(),
            serviceProvider.GetRequiredService<UpdateSalesOrderTool>()
        };
        
        services.AddSingleton<BatchOperationsTool>(sp => new BatchOperationsTool(
            sp.GetRequiredService<ILogger<BatchOperationsTool>>(),
            sp.GetRequiredService<IAuditService>(),
            sp.GetRequiredService<BatchOperationsInputValidator>(),
            sp,
            allTools));
        
        services.AddSingleton<SubscribeWebhookTool>();
        services.AddSingleton<ListWebhooksTool>();
        services.AddSingleton<UnsubscribeWebhookTool>();
        services.AddSingleton<GetRoiMetricsTool>();
        services.AddSingleton<BulkImportTool>();
        
        // Tools - Epic 8: Self-Healing
        services.AddSingleton<GetSelfHealingStatusTool>();
        
        Services = services.BuildServiceProvider();
    }
    
    public void Dispose()
    {
        if (Services is IDisposable disposable)
            disposable.Dispose();
    }
}
