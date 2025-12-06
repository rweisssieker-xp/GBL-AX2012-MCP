using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using GBL.AX2012.MCP.Core.Interfaces;
using GBL.AX2012.MCP.Core.Models;
using GBL.AX2012.MCP.Core.Options;
using GBL.AX2012.MCP.Server.Security;

namespace GBL.AX2012.MCP.Server.Transport;

public class HttpTransportOptions
{
    public const string SectionName = "HttpTransport";
    public int Port { get; set; } = 8080;
    public bool Enabled { get; set; } = true;
    public string[] AllowedOrigins { get; set; } = ["*"];
}

public class HttpTransport : IHostedService
{
    private readonly ILogger<HttpTransport> _logger;
    private readonly HttpTransportOptions _options;
    private readonly IEnumerable<ITool> _tools;
    private readonly IAuthenticationService _authService;
    private readonly IAuthorizationService _authzService;
    private readonly IRateLimiter _rateLimiter;
    private HttpListener? _listener;
    private CancellationTokenSource? _cts;
    
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };
    
    public HttpTransport(
        ILogger<HttpTransport> logger,
        IOptions<HttpTransportOptions> options,
        IEnumerable<ITool> tools,
        IAuthenticationService authService,
        IAuthorizationService authzService,
        IRateLimiter rateLimiter)
    {
        _logger = logger;
        _options = options.Value;
        _tools = tools;
        _authService = authService;
        _authzService = authzService;
        _rateLimiter = rateLimiter;
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("HTTP transport is disabled");
            return Task.CompletedTask;
        }
        
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://+:{_options.Port}/");
        
        try
        {
            _listener.Start();
            _logger.LogInformation("HTTP transport started on port {Port}", _options.Port);
            _ = Task.Run(() => ListenAsync(_cts.Token), _cts.Token);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to start HTTP transport on port {Port}", _options.Port);
        }
        
        return Task.CompletedTask;
    }
    
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _cts?.Cancel();
        _listener?.Stop();
        _logger.LogInformation("HTTP transport stopped");
        return Task.CompletedTask;
    }
    
    private async Task ListenAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _listener?.IsListening == true)
        {
            try
            {
                var context = await _listener.GetContextAsync();
                _ = Task.Run(() => HandleRequestAsync(context, cancellationToken), cancellationToken);
            }
            catch (HttpListenerException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting HTTP connection");
            }
        }
    }
    
    private async Task HandleRequestAsync(HttpListenerContext context, CancellationToken cancellationToken)
    {
        var request = context.Request;
        var response = context.Response;
        
        // CORS
        response.Headers.Add("Access-Control-Allow-Origin", "*");
        response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
        response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");
        
        if (request.HttpMethod == "OPTIONS")
        {
            response.StatusCode = 204;
            response.Close();
            return;
        }
        
        try
        {
            var path = request.Url?.AbsolutePath ?? "/";
            
            var result = path switch
            {
                "/health" => await HandleHealthAsync(cancellationToken),
                "/tools" => HandleToolsList(),
                "/tools/call" when request.HttpMethod == "POST" => await HandleToolCallAsync(request, cancellationToken),
                "/mcp" when request.HttpMethod == "POST" => await HandleMcpRequestAsync(request, cancellationToken),
                _ => new HttpResult(404, new { error = "Not found" })
            };
            
            response.StatusCode = result.StatusCode;
            response.ContentType = "application/json";
            
            var json = JsonSerializer.Serialize(result.Body, _jsonOptions);
            var buffer = Encoding.UTF8.GetBytes(json);
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling HTTP request");
            response.StatusCode = 500;
            var error = JsonSerializer.Serialize(new { error = "Internal server error" });
            var buffer = Encoding.UTF8.GetBytes(error);
            await response.OutputStream.WriteAsync(buffer, cancellationToken);
        }
        finally
        {
            response.Close();
        }
    }
    
    private async Task<HttpResult> HandleHealthAsync(CancellationToken cancellationToken)
    {
        var healthTool = _tools.FirstOrDefault(t => t.Name == "ax_health_check");
        if (healthTool == null)
        {
            return new HttpResult(200, new { status = "healthy" });
        }
        
        var input = JsonSerializer.SerializeToElement(new { includeDetails = true });
        var context = new ToolContext { UserId = "health-check" };
        var result = await healthTool.ExecuteAsync(input, context, cancellationToken);
        
        return new HttpResult(result.Success ? 200 : 503, result.Data);
    }
    
    private HttpResult HandleToolsList()
    {
        var tools = _tools.Select(t => new
        {
            name = t.Name,
            description = t.Description
        });
        
        return new HttpResult(200, new { tools });
    }
    
    private async Task<HttpResult> HandleToolCallAsync(HttpListenerRequest request, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(request.InputStream);
        var body = await reader.ReadToEndAsync(cancellationToken);
        var toolRequest = JsonSerializer.Deserialize<ToolCallRequest>(body, _jsonOptions);
        
        if (toolRequest == null || string.IsNullOrEmpty(toolRequest.Tool))
        {
            return new HttpResult(400, new { error = "Invalid request: tool name required" });
        }
        
        var tool = _tools.FirstOrDefault(t => t.Name == toolRequest.Tool);
        if (tool == null)
        {
            return new HttpResult(404, new { error = $"Tool not found: {toolRequest.Tool}" });
        }
        
        var authResult = await _authService.AuthenticateAsync(cancellationToken);
        var context = new ToolContext
        {
            UserId = authResult.UserId ?? "anonymous",
            Roles = authResult.Roles,
            CorrelationId = Guid.NewGuid().ToString()
        };
        
        if (!await _rateLimiter.TryAcquireAsync(context.UserId, cancellationToken))
        {
            return new HttpResult(429, new { error = "Rate limit exceeded" });
        }
        
        var input = toolRequest.Arguments ?? JsonSerializer.SerializeToElement(new { });
        var result = await tool.ExecuteAsync(input, context, cancellationToken);
        
        return new HttpResult(result.Success ? 200 : 400, result);
    }
    
    private async Task<HttpResult> HandleMcpRequestAsync(HttpListenerRequest request, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(request.InputStream);
        var body = await reader.ReadToEndAsync(cancellationToken);
        
        // Handle MCP JSON-RPC format
        var mcpRequest = JsonSerializer.Deserialize<JsonElement>(body);
        var method = mcpRequest.GetProperty("method").GetString();
        
        return method switch
        {
            "initialize" => new HttpResult(200, new
            {
                jsonrpc = "2.0",
                id = mcpRequest.GetProperty("id"),
                result = new
                {
                    protocolVersion = "2024-11-05",
                    capabilities = new { tools = new { listChanged = false } },
                    serverInfo = new { name = "gbl-ax2012-mcp", version = "1.0.0" }
                }
            }),
            "tools/list" => new HttpResult(200, new
            {
                jsonrpc = "2.0",
                id = mcpRequest.GetProperty("id"),
                result = new { tools = _tools.Select(t => new { name = t.Name, description = t.Description }) }
            }),
            "tools/call" => await HandleMcpToolCallAsync(mcpRequest, cancellationToken),
            _ => new HttpResult(400, new { jsonrpc = "2.0", error = new { code = -32601, message = "Method not found" } })
        };
    }
    
    private async Task<HttpResult> HandleMcpToolCallAsync(JsonElement mcpRequest, CancellationToken cancellationToken)
    {
        var toolName = mcpRequest.GetProperty("params").GetProperty("name").GetString();
        var tool = _tools.FirstOrDefault(t => t.Name == toolName);
        
        if (tool == null)
        {
            return new HttpResult(400, new
            {
                jsonrpc = "2.0",
                id = mcpRequest.GetProperty("id"),
                error = new { code = -32602, message = $"Tool not found: {toolName}" }
            });
        }
        
        var arguments = mcpRequest.GetProperty("params").TryGetProperty("arguments", out var args)
            ? args
            : JsonSerializer.SerializeToElement(new { });
        
        var context = new ToolContext
        {
            UserId = "http-client",
            Roles = new[] { "MCP_Read", "MCP_Write" },
            CorrelationId = Guid.NewGuid().ToString()
        };
        
        var result = await tool.ExecuteAsync(arguments, context, cancellationToken);
        
        return new HttpResult(200, new
        {
            jsonrpc = "2.0",
            id = mcpRequest.GetProperty("id"),
            result = new
            {
                content = new[]
                {
                    new { type = "text", text = JsonSerializer.Serialize(result.Data, _jsonOptions) }
                }
            }
        });
    }
}

public record HttpResult(int StatusCode, object? Body);

public class ToolCallRequest
{
    public string Tool { get; set; } = "";
    public JsonElement? Arguments { get; set; }
}
