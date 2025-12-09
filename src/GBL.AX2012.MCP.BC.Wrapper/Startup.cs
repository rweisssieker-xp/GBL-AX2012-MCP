using System.Web.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Owin;
using GBL.AX2012.MCP.BC.Wrapper.Services;
using GBL.AX2012.MCP.BC.Wrapper.Controllers;

namespace GBL.AX2012.MCP.BC.Wrapper;

public class Startup
{
    public void Configuration(IAppBuilder app)
    {
        var config = new HttpConfiguration();
        
        // Configure dependency injection
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton<BusinessConnectorService>();
        services.AddTransient<HealthCheckController>();
        
        var serviceProvider = services.BuildServiceProvider();
        config.DependencyResolver = new DependencyResolver(serviceProvider);
        
        // Configure Web API routes
        config.MapHttpAttributeRoutes();
        config.Routes.MapHttpRoute(
            name: "DefaultApi",
            routeTemplate: "api/{controller}/{id}",
            defaults: new { id = RouteParameter.Optional }
        );
        
        // Enable CORS
        config.EnableCors();
        
        // JSON formatter
        config.Formatters.JsonFormatter.SerializerSettings.Formatting = Newtonsoft.Json.Formatting.Indented;
        config.Formatters.Remove(config.Formatters.XmlFormatter);
        
        app.UseWebApi(config);
    }
}

// Simple dependency resolver for Web API
public class DependencyResolver : System.Web.Http.Dependencies.IDependencyResolver
{
    private readonly IServiceProvider _serviceProvider;
    
    public DependencyResolver(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    public System.Web.Http.Dependencies.IDependencyScope BeginScope()
    {
        return new DependencyScope(_serviceProvider.CreateScope());
    }
    
    public object GetService(Type serviceType)
    {
        return _serviceProvider.GetService(serviceType);
    }
    
    public IEnumerable<object> GetServices(Type serviceType)
    {
        return _serviceProvider.GetServices(serviceType);
    }
    
    public void Dispose()
    {
        // ServiceProvider handles disposal
    }
}

public class DependencyScope : System.Web.Http.Dependencies.IDependencyScope
{
    private readonly IServiceScope _scope;
    
    public DependencyScope(IServiceScope scope)
    {
        _scope = scope;
    }
    
    public object GetService(Type serviceType)
    {
        return _scope.ServiceProvider.GetService(serviceType);
    }
    
    public IEnumerable<object> GetServices(Type serviceType)
    {
        return _scope.ServiceProvider.GetServices(serviceType);
    }
    
    public void Dispose()
    {
        _scope.Dispose();
    }
}

