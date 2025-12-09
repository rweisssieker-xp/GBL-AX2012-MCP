using Microsoft.Owin.Hosting;
using System;

namespace GBL.AX2012.MCP.BC.Wrapper;

class Program
{
    static void Main(string[] args)
    {
        var port = Environment.GetEnvironmentVariable("BC_WRAPPER_PORT") ?? "8090";
        var baseUrl = $"http://localhost:{port}";
        
        Console.WriteLine($"Starting BC.Wrapper Service on {baseUrl}");
        Console.WriteLine("Press Ctrl+C to stop...");
        
        using (WebApp.Start<Startup>(baseUrl))
        {
            Console.WriteLine($"BC.Wrapper Service is running on {baseUrl}");
            Console.WriteLine($"Health Check: POST {baseUrl}/api/health/check");
            Console.WriteLine($"Status: GET {baseUrl}/api/health/status");
            Console.ReadLine();
        }
    }
}

