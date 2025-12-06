using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Serilog;
using GBL.AX2012.MCP.Core.Interfaces;
using GBL.AX2012.MCP.Core.Options;
using GBL.AX2012.MCP.Server;
using GBL.AX2012.MCP.Server.Middleware;
using GBL.AX2012.MCP.Server.Security;
using GBL.AX2012.MCP.Server.Tools;
using GBL.AX2012.MCP.AxConnector.Clients;
using GBL.AX2012.MCP.AxConnector.Interfaces;
using GBL.AX2012.MCP.Audit.Services;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("MachineName", Environment.MachineName)
    .WriteTo.File(
        path: "logs/mcp-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Starting GBL-AX2012-MCP Server");
    
    var builder = Host.CreateApplicationBuilder(args);
    
    // Add Serilog
    builder.Services.AddSerilog();
    
    // Configure Options
    builder.Services.Configure<McpServerOptions>(builder.Configuration.GetSection(McpServerOptions.SectionName));
    builder.Services.Configure<AifClientOptions>(builder.Configuration.GetSection(AifClientOptions.SectionName));
    builder.Services.Configure<WcfClientOptions>(builder.Configuration.GetSection(WcfClientOptions.SectionName));
    builder.Services.Configure<BusinessConnectorOptions>(builder.Configuration.GetSection(BusinessConnectorOptions.SectionName));
    builder.Services.Configure<RateLimiterOptions>(builder.Configuration.GetSection(RateLimiterOptions.SectionName));
    builder.Services.Configure<CircuitBreakerOptions>(builder.Configuration.GetSection(CircuitBreakerOptions.SectionName));
    builder.Services.Configure<AuditOptions>(builder.Configuration.GetSection(AuditOptions.SectionName));
    builder.Services.Configure<SecurityOptions>(builder.Configuration.GetSection(SecurityOptions.SectionName));
    
    // Register Middleware
    builder.Services.AddSingleton<IRateLimiter, RateLimiter>();
    builder.Services.AddSingleton<ICircuitBreaker, CircuitBreaker>();
    builder.Services.AddSingleton<IIdempotencyStore, MemoryIdempotencyStore>();
    
    // Register Security
    builder.Services.AddSingleton<IAuthenticationService, WindowsAuthenticationService>();
    builder.Services.AddSingleton<IAuthorizationService, AuthorizationService>();
    
    // Register Audit
    builder.Services.AddSingleton<IAuditService, FileAuditService>();
    
    // Register AX Connectors
    builder.Services.AddHttpClient<IAifClient, AifClient>()
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            UseDefaultCredentials = true,
            PreAuthenticate = true
        });
    builder.Services.AddSingleton<IWcfClient, WcfClient>();
    builder.Services.AddSingleton<IBusinessConnector, BusinessConnectorClient>();
    
    // Register Validators
    builder.Services.AddSingleton<GetCustomerInputValidator>();
    builder.Services.AddSingleton<GetSalesOrderInputValidator>();
    builder.Services.AddSingleton<CheckInventoryInputValidator>();
    builder.Services.AddSingleton<SimulatePriceInputValidator>();
    builder.Services.AddSingleton<CreateSalesOrderInputValidator>();
    
    // Register Tools
    builder.Services.AddSingleton<ITool, HealthCheckTool>();
    builder.Services.AddSingleton<ITool, GetCustomerTool>();
    builder.Services.AddSingleton<ITool, GetSalesOrderTool>();
    builder.Services.AddSingleton<ITool, CheckInventoryTool>();
    builder.Services.AddSingleton<ITool, SimulatePriceTool>();
    builder.Services.AddSingleton<ITool, CreateSalesOrderTool>();
    
    // Register MCP Server
    builder.Services.AddHostedService<McpServer>();
    
    var host = builder.Build();
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    Environment.ExitCode = 1;
}
finally
{
    Log.CloseAndFlush();
}
