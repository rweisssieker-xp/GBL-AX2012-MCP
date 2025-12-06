using System.Net;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prometheus;

namespace GBL.AX2012.MCP.Server.Metrics;

public class MetricsServer : IHostedService
{
    private readonly ILogger<MetricsServer> _logger;
    private HttpListener? _listener;
    private CancellationTokenSource? _cts;
    private readonly int _port = 9090;
    
    public MetricsServer(ILogger<MetricsServer> logger)
    {
        _logger = logger;
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://+:{_port}/");
            _listener.Start();
            
            _ = Task.Run(() => ListenAsync(_cts.Token), _cts.Token);
            
            _logger.LogInformation("Prometheus metrics server started on http://localhost:{Port}/metrics", _port);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to start metrics server on port {Port}", _port);
        }
        
        return Task.CompletedTask;
    }
    
    private async Task ListenAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _listener?.IsListening == true)
        {
            try
            {
                var context = await _listener.GetContextAsync();
                await HandleRequestAsync(context);
            }
            catch (HttpListenerException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling metrics request");
            }
        }
    }
    
    private async Task HandleRequestAsync(HttpListenerContext context)
    {
        var response = context.Response;
        
        try
        {
            McpMetrics.UpdateUptime();
            
            using var stream = new MemoryStream();
            await Prometheus.Metrics.DefaultRegistry.CollectAndExportAsTextAsync(stream);
            
            var metrics = Encoding.UTF8.GetString(stream.ToArray());
            var buffer = Encoding.UTF8.GetBytes(metrics);
            
            response.ContentType = "text/plain; version=0.0.4; charset=utf-8";
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing metrics");
            response.StatusCode = 500;
        }
        finally
        {
            response.Close();
        }
    }
    
    public Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            _cts?.Cancel();
            _listener?.Stop();
            _logger.LogInformation("Prometheus metrics server stopped");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error stopping metrics server");
        }
        
        return Task.CompletedTask;
    }
}
